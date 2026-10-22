using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductPriceViewModelService
{
    Task PrepareTierPriceModel(ProductModel.TierPriceModel model);

    //tierprices
    Task<IList<ProductModel.TierPriceModel>> PrepareTierPriceModel(Product product);
}
