using Grand.Business.Core.Interfaces.Catalog.Discounts;

namespace Grand.Business.Catalog.Services.Discounts;

public class DiscountProviderLoader : IDiscountProviderLoader
{
    private readonly IEnumerable<IDiscountAmountProvider> _discountAmountProviders;
    private readonly IEnumerable<IDiscountProvider> _discountProviders;

    public DiscountProviderLoader(IEnumerable<IDiscountProvider> discountProviders,
        IEnumerable<IDiscountAmountProvider> discountAmountProviders)
    {
        _discountProviders = discountProviders;
        _discountAmountProviders = discountAmountProviders;
    }

    /// <summary>
    ///     Load discount provider by rule system name
    /// </summary>
    /// <param name="ruleSystemName">Rule system name</param>
    /// <returns>Found discount</returns>
    public virtual IDiscountProvider LoadDiscountProviderByRuleSystemName(string ruleSystemName)
    {
        var discountPlugins = LoadAllDiscountProviders();
        foreach (var discountPlugin in discountPlugins)
        {
            var rules = discountPlugin.GetRequirementRules();

            if (!rules.Any(x => x.SystemName.Equals(ruleSystemName, StringComparison.OrdinalIgnoreCase)))
                continue;
            return discountPlugin;
        }

        return null;
    }

    /// <summary>
    ///     Load all discount providers
    /// </summary>
    /// <returns>Discount providers</returns>
    public virtual IList<IDiscountProvider> LoadAllDiscountProviders()
    {
        return _discountProviders.ToList();
    }

    /// <summary>
    ///     Get all discount amount providers
    /// </summary>
    /// <returns></returns>
    public virtual IList<IDiscountAmountProvider> LoadDiscountAmountProviders()
    {
        return _discountAmountProviders.ToList();
    }

    /// <summary>
    ///     Load discount amountProviderBySystemName
    /// </summary>
    /// <param name="systemName"></param>
    /// <returns></returns>
    public virtual IDiscountAmountProvider LoadDiscountAmountProviderBySystemName(string systemName)
    {
        return _discountAmountProviders.FirstOrDefault(x =>
            x.SystemName.Equals(systemName, StringComparison.OrdinalIgnoreCase));
    }
}