using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Brands
{
    /// <summary>
    /// Brand service
    /// </summary>
    public partial interface IBrandService
    {

        /// <summary>
        /// Gets all brands
        /// </summary>
        /// <param name="brandName">Brand name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Brands</returns>
        Task<IPagedList<Brand>> GetAllBrands(string brandName = "",
            string storeId = "",
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false);

        /// <summary>
        /// Gets an existing brand by id
        /// </summary>
        /// <param name="brandId">Brand identifier</param>
        /// <returns>Brand</returns>
        Task<Brand> GetBrandById(string brandId);

        /// <summary>
        /// Inserts a new brand
        /// </summary>
        /// <param name="brand">Brand</param>
        Task InsertBrand(Brand brand);

        /// <summary>
        /// Updates the existing brand
        /// </summary>
        /// <param name="brand">Brand</param>
        Task UpdateBrand(Brand brand);

        /// <summary>
        /// Deletes an existing brand
        /// </summary>
        /// <param name="brand">Brand</param>
        Task DeleteBrand(Brand brand);

        /// <summary>
        /// Gets all manfucaturers by discount id
        /// </summary>
        /// <param name="discountId">Discount id </param>
        /// <returns>Product brand mapping</returns>
        Task<IList<Brand>> GetAllBrandsByDiscount(string discountId);

    }
}
