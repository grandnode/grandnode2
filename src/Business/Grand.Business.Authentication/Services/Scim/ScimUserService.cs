using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>
///     SCIM provisioning of staff accounts (<see cref="IScimUserService" />). New accounts are store-less (usable on every store
///     host with per-store identity) and registered without a password anyone knows: they sign in through the identity provider,
///     linked ahead by <see cref="ScimConfig.ExternalIdIsSubject" />. Only staff accounts are taken over by e-mail, and they are
///     not linked ahead: an e-mail at the provider proves nothing about who owns the GrandNode account, so they connect once by
///     the usual rules (password and second factor). No welcome message is sent - SCIM provisions employees, not shoppers.
///     Logs carry customer ids, never e-mails
/// </summary>
public class ScimUserService(
    IRepository<ScimUser> users,
    IRepository<ScimGroup> groups,
    IRepository<ExternalAuthentication> links,
    ICustomerService customerService,
    ICustomerManagerService customerManagerService,
    IGroupService groupService,
    IPermissionService permissionService,
    IOidcCustomerResolver oidcCustomerResolver,
    IOidcLinkService oidcLinkService,
    ScimRoleSync roleSync,
    ScimAccountLock accountLock,
    IMediator mediator,
    ScimConfig config,
    OidcLoginConfig loginConfig,
    CustomerConfig customerConfig,
    ILogger<ScimUserService> logger) : IScimUserService
{
    private const string LinkProvider = OidcCustomerResolver.ProviderSystemName;

    public virtual async Task<ScimUserResult> Create(ScimUserData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var email = EmailOf(data);
        if (email == null)
            return Invalid("userName or emails must hold an e-mail address");

        var externalId = data.ExternalId ?? "";
        if (await ExternalIdTaken(externalId, null))
            return Conflict("A user with this externalId is already provisioned");

        var customer = await customerService.GetCustomerByEmail(email);
        if (customer == null)
        {
            if (await customerService.GetCustomerByUsername(email) != null)
                return Conflict("The user name is already taken");
            return await Register(email, externalId, data);
        }

        var record = await users.GetByIdAsync(customer.Id);
        if (record is { Deprovisioned: false })
            return Conflict("The user is already provisioned");

        //a user the provider deleted and creates again is the same managed account; any other account must be staff
        var refusal = await TakeOverRefusal(customer, externalId, record == null);
        if (refusal != null)
        {
            logger.LogWarning("SCIM refused to take over customer {CustomerId}: {Reason}", customer.Id, refusal);
            return Conflict(refusal);
        }

        if (record == null)
        {
            record = new ScimUser { Id = customer.Id, ExternalId = externalId };
            await roleSync.TakeOver(customer, record);
            if (!await Insert(record))
                return Conflict("The user is already provisioned");
            logger.LogInformation("SCIM took over customer {CustomerId}", customer.Id);
        }
        else
        {
            record.ExternalId = externalId;
            record.Deprovisioned = false;
            await users.UpdateAsync(record);
            await mediator.EntityUpdated(record);
            logger.LogInformation("SCIM provisioned customer {CustomerId} again", customer.Id);
        }

        return await Apply(customer, record, data, email, ScimOutcome.Created);
    }

    public virtual async Task<ScimUserResult> Replace(string id, ScimUserData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var (customer, record) = await Load(id);
        if (customer == null)
            return new ScimUserResult(ScimOutcome.NotFound);

        //every check before the first write: a refused replace changes nothing
        //an account may have become a break-glass account since it was provisioned
        var refusal = ScimProtection.Refusal(customer, loginConfig);
        if (refusal != null)
            return Conflict(refusal);

        var email = EmailOf(data);
        if (email == null)
            return Invalid("userName or emails must hold an e-mail address");

        var emailChanged = email != customer.Email;
        if (emailChanged)
        {
            //the Admin password sign-in of a break-glass address must stay out of the provider's reach
            if (loginConfig.IsBreakGlass(email))
                return Conflict("Break-glass accounts are not managed by SCIM");
            if (OwnedByOther(await customerService.GetCustomerByEmail(email), customer) ||
                OwnedByOther(await customerService.GetCustomerByUsername(email), customer))
                return Conflict("The e-mail address belongs to another account");
        }

        var externalId = data.ExternalId ?? "";
        if (externalId != record.ExternalId && await ExternalIdTaken(externalId, record.Id))
            return Conflict("A user with this externalId is already provisioned");

        if (emailChanged)
        {
            //a user name of its own (UsernamesEnabled) stays; one that was the e-mail follows it
            if (customer.Username == customer.Email)
            {
                await customerService.UpdateCustomerField(customer.Id, x => x.Username, email);
                customer.Username = email;
            }

            await customerService.UpdateCustomerField(customer.Id, x => x.Email, email);
            customer.Email = email;
        }

        if (externalId != record.ExternalId)
        {
            await users.UpdateField(record.Id, x => x.ExternalId, externalId);
            record.ExternalId = externalId;
            await mediator.EntityUpdated(record);
        }

        return await Apply(customer, record, data, email, ScimOutcome.Ok);
    }

    public virtual async Task<ScimUserResult> Get(string id)
    {
        var (customer, record) = await Load(id);
        return customer == null
            ? new ScimUserResult(ScimOutcome.NotFound)
            : new ScimUserResult(ScimOutcome.Ok, await View(customer, record));
    }

    public virtual async Task<ScimPage<ScimUserView>> List(string userName, string externalId, int skip, int take)
    {
        var query = users.Table.Where(x => !x.Deprovisioned);
        if (!string.IsNullOrEmpty(externalId))
            query = query.Where(x => x.ExternalId == externalId);
        if (!string.IsNullOrEmpty(userName))
        {
            //the SCIM user name is the account's e-mail
            var customer = await customerService.GetCustomerByEmail(userName.Trim());
            if (customer == null)
                return new ScimPage<ScimUserView>([], 0);
            var customerId = customer.Id;
            query = query.Where(x => x.Id == customerId);
        }

        var total = await users.CountAsync(query);
        var records = take <= 0
            ? []
            : await users.ToListAsync(query.OrderBy(x => x.CreatedOnUtc).ThenBy(x => x.Id).Skip(Math.Max(skip, 0)).Take(take));
        var customers = records.Count == 0
            ? new Dictionary<string, Customer>()
            : (await customerService.GetCustomersByIds(records.Select(x => x.Id).ToArray())).ToDictionary(x => x.Id);

        var items = new List<ScimUserView>();
        foreach (var record in records)
            if (customers.TryGetValue(record.Id, out var customer) && !customer.Deleted)
                items.Add(await View(customer, record));
        return new ScimPage<ScimUserView>(items, total);
    }

    /// <summary>
    ///     The account is deactivated, loses the roles SCIM granted, its OpenID Connect link and its groups; the record stays,
    ///     marked deprovisioned, so the same user created again is recognised (and reactivated when SCIM deactivated it)
    /// </summary>
    public virtual async Task<ScimOutcome> Delete(string id)
    {
        var (customer, record) = await Load(id);
        if (customer == null)
            return ScimOutcome.NotFound;

        //an account may have become a break-glass account since it was provisioned
        var refusal = ScimProtection.Refusal(customer, loginConfig);
        if (refusal != null)
        {
            logger.LogWarning("SCIM refused to delete customer {CustomerId}: {Reason}", customer.Id, refusal);
            return ScimOutcome.Conflict;
        }

        if (customer.Active)
        {
            await accountLock.Deactivate(customer);
            await users.UpdateField(record.Id, x => x.DeactivatedByScim, true);
        }

        await roleSync.Revoke(customer, record);

        var link = await links.GetOneAsync(x => x.ProviderSystemName == LinkProvider && x.CustomerId == id);
        if (link != null)
            await oidcLinkService.Remove(customer, link.Id);

        foreach (var group in await groups.ToListAsync(groups.Table.Where(x => x.Members.Contains(id))))
            await groups.RemoveCollectionFieldItem(group.Id, x => x.Members, m => m == id);

        await users.UpdateField(record.Id, x => x.Deprovisioned, true);
        record.Deprovisioned = true;
        await mediator.EntityUpdated(record);
        logger.LogInformation("SCIM user {CustomerId} deleted; the customer is kept inactive", id);
        return ScimOutcome.Ok;
    }

    public virtual async Task<IList<string>> GetConflicts() =>
        await users.ToListAsync(users.Table.Where(x => x.RoleConflict && !x.Deprovisioned).Select(x => x.Id));

    public virtual Task<int> Count() => users.CountAsync(users.Table.Where(x => !x.Deprovisioned));

    private async Task<ScimUserResult> Apply(Customer customer, ScimUser record, ScimUserData data, string email,
        ScimOutcome outcome)
    {
        //an empty value removes the user field
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, data.GivenName ?? "");
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, data.FamilyName ?? "");

        if (!data.Active)
        {
            //an account disabled by hand stays the administrator's: SCIM does not take it over as its own deactivation
            if (customer.Active)
            {
                await accountLock.Deactivate(customer);
                await SetDeactivatedByScim(record, true);
            }
        }
        else if (!customer.Active && record.DeactivatedByScim)
        {
            await accountLock.Activate(customer);
            await SetDeactivatedByScim(record, false);
        }

        await EnsureLink(customer, record, email);
        await roleSync.Sync(await customerService.GetCustomerById(customer.Id) ?? customer, record);

        var stored = await customerService.GetCustomerById(customer.Id) ?? customer;
        return new ScimUserResult(outcome, await View(stored, record));
    }

    //the one field alone: a whole-record write would undo a concurrent role sync
    private async Task SetDeactivatedByScim(ScimUser record, bool value)
    {
        await users.UpdateField(record.Id, x => x.DeactivatedByScim, value);
        record.DeactivatedByScim = value;
        await mediator.EntityUpdated(record);
    }

    /// <summary>
    ///     An account SCIM created is linked to its externalId (the OpenID Connect subject) as if connected with its password, so
    ///     the first sign-in needs none. A taken-over account is not: it connects at sign-in by the usual rules. A link to another
    ///     subject is kept (the administrator decides) and only logged
    /// </summary>
    private async Task EnsureLink(Customer customer, ScimUser record, string email)
    {
        if (!record.RegisteredByScim || !config.ExternalIdIsSubject || string.IsNullOrEmpty(record.ExternalId))
            return;

        var customerId = customer.Id;
        var link = await links.GetOneAsync(x => x.ProviderSystemName == LinkProvider && x.CustomerId == customerId);
        if (link != null && link.ExternalIdentifier != record.ExternalId)
        {
            logger.LogWarning("SCIM user {CustomerId} is linked to another OpenID Connect subject; the link is kept",
                customerId);
            return;
        }

        if (link == null)
        {
            var result = await oidcCustomerResolver.LinkIdentity(customer, record.ExternalId, email, "");
            if (result != OidcLinkResult.Linked)
                logger.LogWarning("SCIM could not link customer {CustomerId} to its externalId ({Result})", customerId,
                    result);
        }
    }

    private async Task<ScimUserResult> Register(string email, string externalId, ScimUserData data)
    {
        var customer = await customerService.InsertGuestCustomer(new Customer {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            StoreId = "",
            LastActivityDateUtc = DateTime.UtcNow
        });

        //a password nobody knows: the account signs in through the identity provider
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await customerManagerService.RegisterCustomer(new RegistrationRequest(customer, email, email, password,
            PasswordFormat.Hashed, ""));

        var record = new ScimUser { Id = customer.Id, ExternalId = externalId, RegisteredByScim = true };
        if (!await Insert(record))
            return Conflict("The user is already provisioned");
        logger.LogInformation("SCIM registered customer {CustomerId}", customer.Id);
        return await Apply(customer, record, data, email, ScimOutcome.Created);
    }

    //false when a concurrent request provisioned the same account first (the record id is the customer id)
    private async Task<bool> Insert(ScimUser record)
    {
        try
        {
            await users.InsertAsync(record);
        }
        catch (Exception ex)
        {
            logger.LogInformation("Storing the SCIM user of customer {CustomerId} failed ({ExceptionType}); re-reading",
                record.Id, ex.GetType().Name);
            if (await users.GetByIdAsync(record.Id) == null)
                throw;
            return false;
        }

        await mediator.EntityInserted(record);
        return true;
    }

    /// <param name="mustBeStaff">False for a user the provider deleted and creates again: it was managed already</param>
    private async Task<string> TakeOverRefusal(Customer customer, string externalId, bool mustBeStaff)
    {
        var refusal = ScimProtection.Refusal(customer, loginConfig);
        if (refusal != null)
            return refusal;

        if (customerConfig.RegisterCustomersPerStore && !string.IsNullOrEmpty(customer.StoreId))
            return "The e-mail address belongs to an account of one store";

        if (!await groupService.IsRegistered(customer))
            return "Only registered accounts can be managed by SCIM";

        //a shopper with the e-mail is not the provider's to manage
        if (mustBeStaff && !await permissionService.IsBackOfficeAccount(customer))
            return "Only staff accounts are taken over by SCIM";

        if (!config.ExternalIdIsSubject || externalId == "")
            return null;

        var customerId = customer.Id;
        var link = await links.GetOneAsync(x => x.ProviderSystemName == LinkProvider && x.CustomerId == customerId);
        if (link != null && link.ExternalIdentifier != externalId)
            return "The account is linked to another identity";

        var subjectLink = await links.GetOneAsync(x =>
            x.ProviderSystemName == LinkProvider && x.ExternalIdentifier == externalId && x.CustomerId != customerId);
        return subjectLink != null ? "The identity is linked to another account" : null;
    }

    //a deprovisioned user keeps its externalId for a re-creation, so it does not block another user
    private async Task<bool> ExternalIdTaken(string externalId, string exceptId) =>
        externalId != "" &&
        await users.GetOneAsync(x => x.ExternalId == externalId && !x.Deprovisioned && x.Id != exceptId) != null;

    private async Task<(Customer Customer, ScimUser Record)> Load(string id)
    {
        if (string.IsNullOrEmpty(id))
            return (null, null);
        var record = await users.GetByIdAsync(id);
        if (record is not { Deprovisioned: false })
            return (null, null);
        var customer = await customerService.GetCustomerById(id);
        return customer is { Deleted: false } ? (customer, record) : (null, null);
    }

    private async Task<ScimUserView> View(Customer customer, ScimUser record)
    {
        var customerId = customer.Id;
        var memberOf = await groups.ToListAsync(groups.Table.Where(x => x.Members.Contains(customerId)));
        return new ScimUserView(
            customer.Id,
            string.IsNullOrEmpty(record.ExternalId) ? null : record.ExternalId,
            customer.Email,
            customer.Email,
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
            customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName),
            customer.Active,
            memberOf.Select(x => new ScimGroupRef(x.Id, x.DisplayName)).ToList(),
            record.CreatedOnUtc,
            record.UpdatedOnUtc ?? record.CreatedOnUtc);
    }

    private static bool OwnedByOther(Customer found, Customer customer) => found != null && found.Id != customer.Id;

    //the first of userName and the e-mails that is an e-mail address, lower-cased
    private static string EmailOf(ScimUserData data) =>
        new[] { data.UserName, data.Email }
            .Select(x => x?.Trim())
            .FirstOrDefault(x => !string.IsNullOrEmpty(x) && CommonHelper.IsValidEmail(x))
            ?.ToLowerInvariant();

    private static ScimUserResult Conflict(string detail) => new(ScimOutcome.Conflict, Detail: detail);
    private static ScimUserResult Invalid(string detail) => new(ScimOutcome.Invalid, Detail: detail);
}
