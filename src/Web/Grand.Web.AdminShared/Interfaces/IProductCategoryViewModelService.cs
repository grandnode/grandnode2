using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductCategoryViewModelService
{
    Task<IList<ProductModel.ProductCategoryModel>> PrepareProductCategoryModel(Product product);

    Task InsertProductCategoryModel(ProductModel.ProductCategoryModel model);

    Task UpdateProductCategoryModel(ProductModel.ProductCategoryModel model);

    Task DeleteProductCategory(string id, string productId);
}
