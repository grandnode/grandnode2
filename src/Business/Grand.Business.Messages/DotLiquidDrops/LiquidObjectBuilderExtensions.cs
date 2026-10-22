using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Messages.DotLiquidDrops;

internal static class LiquidObjectBuilderExtensions
{
    public static void AddCustomerTokensIfNotNull(this LiquidObjectBuilder builder, Customer customer, Store store,
        DomainHost host, Language language)
    {
        if (customer != null)
        {
            builder.AddCustomerTokens(customer, store, host, language);
        }
    }
}
