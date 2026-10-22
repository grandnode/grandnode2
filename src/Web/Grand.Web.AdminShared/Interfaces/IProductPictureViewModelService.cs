using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductPictureViewModelService
{
    //Pictures
    Task<IList<ProductModel.ProductPictureModel>> PrepareProductPicturesModel(Product product);

    Task<(ProductModel.ProductPictureModel model, Picture Picture)> PrepareProductPictureModel(Product product,
        ProductPicture productPicture);

    Task InsertProductPicture(Product product, Picture picture, int displayOrder);

    Task UpdateProductPicture(ProductModel.ProductPictureModel model);

    Task DeleteProductPicture(ProductModel.ProductPictureModel model);
}
