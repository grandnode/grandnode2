namespace Grand.Business.Core.Interfaces.Catalog.Discounts;

public interface IDiscountProviderLoader
{
    /// <summary>
    ///     Loads existing discount provider by rule system name
    /// </summary>
    /// <param name="ruleSystemName">Rule system name</param>
    /// <returns>Discount provider</returns>
    IDiscountProvider LoadDiscountProviderByRuleSystemName(string ruleSystemName);

    /// <summary>
    ///     Loads all available discount providers
    /// </summary>
    /// <returns>Discount requirement rules</returns>
    IList<IDiscountProvider> LoadAllDiscountProviders();


    /// <summary>
    ///     Load discount amount providers
    /// </summary>
    /// <returns></returns>
    IList<IDiscountAmountProvider> LoadDiscountAmountProviders();

    /// <summary>
    ///     Load discount amount providerBySystemName
    /// </summary>
    /// <param name="systemName"></param>
    /// <returns></returns>
    IDiscountAmountProvider LoadDiscountAmountProviderBySystemName(string systemName);
}