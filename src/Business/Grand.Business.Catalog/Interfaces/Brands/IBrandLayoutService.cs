using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Brands
{
    public partial interface IBrandLayoutService
    {

        /// <summary>
        /// Gets all existing brand layouts
        /// </summary>
        /// <returns>Brand layouts</returns>
        Task<IList<BrandLayout>> GetAllBrandLayouts();

        /// <summary>
        /// Gets an existing brand layout
        /// </summary>
        /// <param name="brandLayoutId">Brand layout id</param>
        /// <returns>Brand layout</returns>
        Task<BrandLayout> GetBrandLayoutById(string brandLayoutId);

        /// <summary>
        /// Inserts a new brand layout
        /// </summary>
        /// <param name="brandLayout">Brand layout</param>
        Task InsertBrandLayout(BrandLayout brandLayout);

        /// <summary>
        /// Updates the existing brand layout
        /// </summary>
        /// <param name="brandLayout">Brand layout</param>
        Task UpdateBrandLayout(BrandLayout brandLayout);
        /// <summary>
        /// Deletes existing brand layout
        /// </summary>
        /// <param name="brandLayout">Brand layout</param>
        Task DeleteBrandLayout(BrandLayout brandLayout);
    }
}
