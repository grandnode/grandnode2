using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class ProductAddPopupViewModelService(
    IProductService productService,
    IDateTimeService dateTimeService,
    IVendorService vendorService,
    IStoreService storeService,
    IRequestTranslationService translationService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : IProductAddPopupViewModelService
{
    public virtual async Task<ProductModel.AddRequiredProductModel> PrepareAddRequiredProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddRequiredProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddRelatedProductModel> PrepareRelatedProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddRelatedProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddSimilarProductModel> PrepareSimilarProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddSimilarProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddBundleProductModel> PrepareBundleProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddBundleProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddCrossSellProductModel> PrepareCrossSellProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddCrossSellProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddRecommendedProductModel> PrepareRecommendedProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddRecommendedProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.AddAssociatedProductModel> PrepareAssociatedProductModel()
    {
        var model = await PrepareAddProductModel<ProductModel.AddAssociatedProductModel>();
        return model;
    }

    public virtual async Task<ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel> PrepareAssociateProductToAttributeValueModel()
    {
        var model = await PrepareAddProductModel<ProductModel.ProductAttributeValueModel.AssociateProductToAttributeValueModel>();
        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        ProductModel.AddProductModel model, int pageIndex, int pageSize)
    {
        // vendorId: scope.DefaultVendorId (when set) overrides the search model - mirrors Vendor's original
        // PrepareProductModel(AddProductModel,...), which always passed vendorId: CurrentVendor.Id (and
        // storeId: string.Empty) regardless of the model, so the "add related/similar/bundle/etc. product"
        // popups can never surface another vendor's products.
        var products = await productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, scope.DefaultVendorId ?? model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel(dateTimeService)).ToList(), products.TotalCount);
    }

    protected virtual async Task<T> PrepareAddProductModel<T>() where T : ProductModel.AddProductModel, new()
    {
        var storeId = scope.DefaultStoreId ?? "";
        var model = new T();

        //stores - same capability gate as PrepareProductListModel: Vendor's original PrepareAddProductModel<T>
        //never populated a stores dropdown at all (vendors don't pick stores), so this must not run for
        //Vendor even though its storeId also resolves to "".
        if (scope.ShowStoreSelector)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in (await storeService.GetAllStores()).Where(x =>
                         x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        }

        //vendors - only when the host isn't already forced onto a single vendor (scope.DefaultVendorId):
        //Vendor's original PrepareAddProductModel<T> never populated a vendor picker at all (matches the
        //ShowStoreSelector gate above for the same reason - there's nothing to pick, the search is always
        //forced to the current vendor).
        if (scope.DefaultVendorId is null)
        {
            model.AvailableVendors.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });
        }

        //product types
        model.AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "0" });

        return model;
    }
}
