using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;

namespace Grand.Business.Authentication.Services.Scim;

/// <summary>Accounts SCIM never manages, whatever the identity provider sends</summary>
internal static class ScimProtection
{
    /// <summary>Why SCIM must leave the account alone; null when it may manage it</summary>
    public static string Refusal(Customer customer, OidcLoginConfig loginConfig)
    {
        if (customer.Deleted)
            return "The account is deleted";
        if (customer.IsSystemAccount || !string.IsNullOrEmpty(customer.SystemName))
            return "System accounts are not managed by SCIM";
        return loginConfig.IsBreakGlass(customer.Email) ? "Break-glass accounts are not managed by SCIM" : null;
    }
}
