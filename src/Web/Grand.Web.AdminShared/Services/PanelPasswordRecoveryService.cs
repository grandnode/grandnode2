using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

public class PanelPasswordRecoveryService(
    ICustomerService customerService,
    ICustomerManagerService customerManagerService,
    ICustomerHistoryPasswordService customerHistoryPasswordService,
    IGroupService groupService,
    IPermissionService permissionService,
    IVendorService vendorService,
    IMessageProviderService messageProviderService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    CustomerSettings customerSettings) : IPanelPasswordRecoveryService
{
    public const string AreaAdmin = "Admin";
    public const string AreaStore = "Store";
    public const string AreaVendor = "Vendor";

    public async Task SendRecoveryMessage(string email, string area)
    {
        var customer = await GetPanelCustomer(email, area);
        if (customer == null)
            return;

        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordRecoveryToken,
            Guid.NewGuid().ToString());
        await customerService.UpdateUserField<DateTime?>(customer,
            SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated, DateTime.UtcNow);
        //read by Customer.PasswordRecoveryURL, so the link leads back to this panel
        await customerService.UpdateUserField(customer, SystemCustomerFieldNames.PasswordRecoveryArea,
            area.ToLowerInvariant());

        await messageProviderService.SendCustomerPasswordRecoveryMessage(customer,
            contextAccessor.StoreContext.CurrentStore, contextAccessor.WorkContext.WorkingLanguage.Id);
    }

    public async Task<string> ValidateToken(string email, string token, string area)
    {
        var customer = await GetPanelCustomer(email, area);
        return CheckToken(customer, token);
    }

    public async Task<string> ResetPassword(string email, string token, string area, string newPassword)
    {
        var customer = await GetPanelCustomer(email, area);
        var error = CheckToken(customer, token);
        if (error != null)
            return error;

        if (customerSettings.UnduplicatedPasswordsNumber > 0)
        {
            var previousPasswords = await customerHistoryPasswordService.GetPasswords(customer.Id,
                customerSettings.UnduplicatedPasswordsNumber);
            if (previousPasswords.Any(password => customerManagerService.PasswordMatch(
                    customerSettings.DefaultPasswordFormat, password.Password, newPassword, password.PasswordSalt)))
                return translationService.GetResource("Account.PasswordRecovery.PasswordMatchesWithPrevious");
        }

        await customerManagerService.ChangePassword(new ChangePasswordRequest(customer.Email,
            customerSettings.DefaultPasswordFormat, newPassword));

        //a link works once
        await customerService.UpdateUserField<string>(customer, SystemCustomerFieldNames.PasswordRecoveryToken, null);
        await customerService.UpdateUserField<string>(customer, SystemCustomerFieldNames.PasswordRecoveryArea, null);
        return null;
    }

    private string CheckToken(Customer customer, string token)
    {
        //an unknown account reads as a wrong link, so the page does not tell which emails exist
        if (customer == null || string.IsNullOrEmpty(token) || !customer.IsPasswordRecoveryTokenValid(token))
            return translationService.GetResource("Account.PasswordRecovery.WrongToken");

        return customer.IsPasswordRecoveryLinkExpired(customerSettings)
            ? translationService.GetResource("Account.PasswordRecovery.LinkExpired")
            : null;
    }

    /// <summary>
    ///     The account behind the email when it may sign in to the panel, otherwise null. The panel checks mirror
    ///     AuthorizeAdminAttribute, AuthorizeStoreAttribute and AuthorizeVendorAttribute; the lookup is the one
    ///     the panels' LoginValidator uses.
    /// </summary>
    private async Task<Customer> GetPanelCustomer(string email, string area)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var customer = await customerService.GetCustomerByEmail(email);
        if (customer is not { Deleted: false, Active: true })
            return null;
        if (customer.CannotLoginUntilDateUtc.HasValue && customer.CannotLoginUntilDateUtc.Value > DateTime.UtcNow)
            return null;
        if (!await groupService.IsRegistered(customer))
            return null;

        return await HasPanelAccess(customer, area) ? customer : null;
    }

    private async Task<bool> HasPanelAccess(Customer customer, string area)
    {
        switch (area)
        {
            case AreaAdmin:
                return await permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer)
                       && string.IsNullOrEmpty(customer.StaffStoreId)
                       && string.IsNullOrEmpty(customer.VendorId)
                       && !await groupService.IsStoreManager(customer)
                       && !await groupService.IsVendor(customer);
            case AreaStore:
                return await permissionService.Authorize(StandardPermission.ManageAccessStoreManagerPanel, customer)
                       && !string.IsNullOrEmpty(customer.StaffStoreId)
                       && await groupService.IsStoreManager(customer);
            case AreaVendor:
                if (string.IsNullOrEmpty(customer.VendorId)
                    || !await permissionService.Authorize(StandardPermission.ManageAccessVendorPanel, customer)
                    || !await groupService.IsVendor(customer))
                    return false;
                var vendor = await vendorService.GetVendorById(customer.VendorId);
                return vendor is { Active: true, Deleted: false };
            default:
                return false;
        }
    }
}
