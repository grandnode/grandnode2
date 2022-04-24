using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Catalog.Discounts
{
    public partial interface IDiscountProvider : IProvider
    {
        IList<IDiscountRule> GetRequirementRules();
    }
}
