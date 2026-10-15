using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     What a guest cookie may resolve to
/// </summary>
public static class GuestCustomerRules
{
    /// <summary>
    ///     The cookie holds a GUID in the format the store writes ("D"), never Guid.Empty
    /// </summary>
    public static bool TryParseCookie(string value, out Guid customerGuid)
    {
        return Guid.TryParseExact(value, "D", out customerGuid) && customerGuid != Guid.Empty;
    }

    /// <summary>
    ///     An active guest: not deleted, not registered and not a system account (Anonymous and SearchEngine are
    ///     in the Guests group too)
    /// </summary>
    public static async Task<bool> IsAcceptableGuest(Customer customer, IGroupService groupService)
    {
        return customer is { Deleted: false, Active: true, IsSystemAccount: false }
               && !await groupService.IsRegistered(customer);
    }
}
