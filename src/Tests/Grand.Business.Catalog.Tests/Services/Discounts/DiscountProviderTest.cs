using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Utilities.Catalog;

namespace Grand.Business.Catalog.Tests.Services.Discounts
{
    public class DiscountProviderTest : IDiscountProvider
    {
        private IList<string> limitedToStores;
        private IList<string> limitedToGroups;
        public DiscountProviderTest()
        {
            limitedToStores = new List<string>();
            limitedToGroups = new List<string>();
        }

        public string ConfigurationUrl => throw new NotImplementedException();

        public string SystemName => "SampleDiscountProvider";

        public string FriendlyName => throw new NotImplementedException();

        public int Priority => throw new NotImplementedException();

        public IList<string> LimitedToStores => limitedToStores;

        public IList<string> LimitedToGroups => limitedToGroups;

        public IList<IDiscountRule> GetRequirementRules()
        {
            return new List<IDiscountRule> { new DiscountRuleValidTest() };
        }
    }

    public class DiscountRuleValidTest : IDiscountRule
    {
        public string SystemName => "RuleSystemName";

        public string FriendlyName => throw new NotImplementedException();

        public async Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request)
        {
            var result = new DiscountRuleValidationResult() {
                IsValid = true
            };
            return await Task.FromResult(result);
        }

        public string GetConfigurationUrl(string discountId, string discountRequirementId)
        {
            throw new NotImplementedException();
        }
    }

}
