using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductAssociatedViewModelService
{
    Task InsertAssociatedProductModel(ProductModel.AddAssociatedProductModel model);

    Task DeleteAssociatedProduct(Product product);
}
