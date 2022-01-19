using Grand.Infrastructure.Plugins;

namespace Grand.Business.Catalog.Interfaces.Discounts
{
    public partial interface IDiscountProvider : IProvider
    {
        IList<IDiscountRule> GetRequirementRules();
    }
}
