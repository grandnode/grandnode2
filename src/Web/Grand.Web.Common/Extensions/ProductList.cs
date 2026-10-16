using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Web.Common.Extensions;

public static class ProductList
{
    public static async Task<IPagedList<Product>> PrepareProductList(this IProductService productService,
        string searchCategoryId,
        string searchBrandId, string searchCollectionId, string searchStoreId, string searchVendorId, int productTypeId,
        string searchProductName, int pageIndex, int pageSize)
    {
        var searchCategoryIds = new List<string>();
        if (!string.IsNullOrEmpty(searchCategoryId))
            searchCategoryIds.Add(searchCategoryId);

        var products = (await productService.SearchProducts(
            access: CatalogAccess.IncludingHidden(searchStoreId),
            categoryIds: searchCategoryIds,
            brandId: searchBrandId,
            collectionId: searchCollectionId,
            vendorId: searchVendorId,
            productType: productTypeId > 0 ? (ProductType?)productTypeId : null,
            keywords: searchProductName,
            pageIndex: pageIndex - 1,
            pageSize: pageSize)).products;

        return products;
    }
}