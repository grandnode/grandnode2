using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductCollectionViewModelService
{
    Task<IList<ProductModel.ProductCollectionModel>> PrepareProductCollectionModel(Product product);

    Task InsertProductCollection(ProductModel.ProductCollectionModel model);

    Task UpdateProductCollection(ProductModel.ProductCollectionModel model);

    Task DeleteProductCollection(string id, string productId);
}
