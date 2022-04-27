using Grand.Business.Core.Utilities.Catalog;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Catalog.Tax
{
    /// <summary>
    /// Provides an interface for creating tax providers
    /// </summary>
    public partial interface ITaxProvider : IProvider
    {
        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        Task<TaxResult> GetTaxRate(TaxRequest calculateTaxRequest);

    }
}
