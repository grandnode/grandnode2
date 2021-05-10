using Grand.Business.Catalog.Utilities;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Tax
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
