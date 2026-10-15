using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <remarks>
///     Takes the undecorated customer service: the decorator depends on this class
/// </remarks>
public class GuestCustomerPersistence(
    ICustomerService customerService,
    IGroupService groupService,
    ILogger<GuestCustomerPersistence> logger) : IGuestCustomerPersistence
{
    private Task _persisting;

    public Customer Transient { get; private set; }

    public async Task SetTransient(Customer customer)
    {
        //the Guests group is normally added on insert; permission checks read it from the unsaved customer
        var guestsGroup = await groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Guests);
        if (guestsGroup != null && !customer.Groups.Contains(guestsGroup.Id))
            customer.Groups.Add(guestsGroup.Id);

        Transient = customer;
        _persisting = null;
    }

    public bool IsTransient(string customerId)
    {
        return Transient != null && !string.IsNullOrEmpty(customerId) && Transient.Id == customerId;
    }

    public Task EnsurePersisted()
    {
        if (Transient == null) return Task.CompletedTask;

        //writes of one request may run concurrently: they share the single insert
        return _persisting ??= Persist(Transient);
    }

    private async Task Persist(Customer guest)
    {
        await customerService.InsertGuestCustomer(guest);
        Transient = null;
    }

    public async Task<Customer> Promote(Customer guest)
    {
        try
        {
            return await customerService.InsertGuestCustomer(guest);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            //the unique CustomerGuid index: a parallel request of the same browser saved this cookie first
            var existing = await customerService.GetCustomerByGuid(guest.CustomerGuid);
            if (existing == null) throw;

            if (await GuestCustomerRules.IsAcceptableGuest(existing, groupService)) return existing;

            logger.LogWarning(ex, "Guest cookie {CustomerGuid} belongs to a customer that is not a guest; a new guest is issued",
                guest.CustomerGuid);
            return null;
        }
    }
}
