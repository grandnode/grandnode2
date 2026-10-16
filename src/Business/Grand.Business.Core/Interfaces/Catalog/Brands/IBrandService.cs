using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Brands;

/// <summary>
///     Brand service
/// </summary>
public interface IBrandService
{
    /// <summary>
    ///     Gets all brands
    /// </summary>
    /// <param name="brandName">Brand name</param>
    /// <param name="access">Whose view of the brands to return, and the store whose limitations apply</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Brands</returns>
    Task<IPagedList<Brand>> GetAllBrands(string brandName,
        CatalogAccess access,
        int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets an existing brand by id
    /// </summary>
    /// <param name="brandId">Brand identifier</param>
    /// <returns>Brand</returns>
    Task<Brand> GetBrandById(string brandId);

    /// <summary>
    ///     Inserts a new brand
    /// </summary>
    /// <param name="brand">Brand</param>
    Task InsertBrand(Brand brand);

    /// <summary>
    ///     Updates the existing brand
    /// </summary>
    /// <param name="brand">Brand</param>
    Task UpdateBrand(Brand brand);

    /// <summary>
    ///     Deletes an existing brand
    /// </summary>
    /// <param name="brand">Brand</param>
    Task DeleteBrand(Brand brand);

    /// <summary>
    ///     Gets all brands by discount id
    /// </summary>
    /// <param name="discountId">Discount id </param>
    /// <returns>Product brand mapping</returns>
    Task<IList<Brand>> GetAllBrandsByDiscount(string discountId);
}