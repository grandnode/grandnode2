using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Interfaces;

public interface IProductBundleViewModelService
{
    Task InsertBundleProductModel(ProductModel.AddBundleProductModel model);

    Task UpdateBundleProductModel(ProductModel.BundleProductModel model);

    Task DeleteBundleProductModel(ProductModel.BundleProductModel model);
}
