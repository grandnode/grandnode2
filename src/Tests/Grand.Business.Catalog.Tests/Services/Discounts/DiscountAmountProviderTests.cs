using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;

namespace Grand.Business.Catalog.Tests.Services.Discounts;

public class DiscountAmountProviderTests : IDiscountAmountProvider
{
    public string ConfigurationUrl => throw new NotImplementedException();

    public string SystemName => "SampleDiscountAmountProvider";

    public string FriendlyName => throw new NotImplementedException();

    public int Priority => throw new NotImplementedException();

    public IList<string> LimitedToStores => new List<string>();

    public IList<string> LimitedToGroups => new List<string>();

    public async Task<double> DiscountAmount(Discount discount, Customer customer, Product product, double amount)
    {
        return await Task.FromResult(9);
    }
}