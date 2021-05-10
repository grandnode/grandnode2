using Grand.Infrastructure.Plugins;
using System.Collections.Generic;

namespace Grand.Business.Catalog.Interfaces.Discounts
{
    public partial interface IDiscountProvider : IProvider
    {
        IList<IDiscountRule> GetRequirementRules();
    }
}
