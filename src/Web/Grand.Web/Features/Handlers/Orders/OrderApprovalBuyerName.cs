using Grand.Domain.Common;
using Grand.Domain.Customers;

namespace Grand.Web.Features.Handlers.Orders;

internal static class OrderApprovalBuyerName
{
    /// <summary>
    ///     Gets the name an approver recognizes the buyer by: the full name and the e-mail, whichever exist
    /// </summary>
    public static string For(Customer buyer)
    {
        if (buyer == null) return null;

        var name = $"{buyer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName)} " +
                   $"{buyer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)}";
        name = name.Trim();
        if (string.IsNullOrEmpty(name)) return buyer.Email;
        return string.IsNullOrEmpty(buyer.Email) ? name : $"{name} ({buyer.Email})";
    }
}
