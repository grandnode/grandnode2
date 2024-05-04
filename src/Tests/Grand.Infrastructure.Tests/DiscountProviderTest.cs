using Grand.Business.Core.Interfaces.Catalog.Discounts;

namespace Grand.Infrastructure.Tests;

public class DiscountProviderTest : IDiscountProvider
{
    public DiscountProviderTest(List<string> stores, IList<string> groups)
    {
        LimitedToStores = stores;
        LimitedToGroups = groups;
    }

    public string ConfigurationUrl => throw new NotImplementedException();

    public string SystemName => throw new NotImplementedException();

    public string FriendlyName => throw new NotImplementedException();

    public int Priority => throw new NotImplementedException();

    public IList<string> LimitedToStores { get; }

    public IList<string> LimitedToGroups { get; }

    public IList<IDiscountRule> GetRequirementRules()
    {
        throw new NotImplementedException();
    }
}