using Grand.Business.Core.Interfaces.Catalog.Discounts;

namespace Grand.Infrastructure.Tests
{
    public class DiscountProviderTest : IDiscountProvider
    {
        private IList<string> limitedToStores;
        private IList<string> limitedToGroups;
        public DiscountProviderTest(List<string> stores, IList<string> groups)
        {
            limitedToStores = stores;
            limitedToGroups = groups;
        }

        public string ConfigurationUrl => throw new NotImplementedException();

        public string SystemName => throw new NotImplementedException();

        public string FriendlyName => throw new NotImplementedException();

        public int Priority => throw new NotImplementedException();

        public IList<string> LimitedToStores => limitedToStores;

        public IList<string> LimitedToGroups => limitedToGroups;

        public IList<IDiscountRule> GetRequirementRules()
        {
            throw new NotImplementedException();
        }
    }
}
