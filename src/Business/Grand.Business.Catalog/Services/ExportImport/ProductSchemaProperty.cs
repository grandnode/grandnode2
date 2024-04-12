using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.ExportImport;

public class ProductSchemaProperty : ISchemaProperty<Product>
{
    private readonly IPictureService _pictureService;

    public ProductSchemaProperty(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public virtual async Task<PropertyByName<Product>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Product>("Id", p => p.Id),
            new PropertyByName<Product>("ProductTypeId", p => (int)p.ProductTypeId),
            new PropertyByName<Product>("ParentGroupedProductId", p => p.ParentGroupedProductId),
            new PropertyByName<Product>("VisibleIndividually", p => p.VisibleIndividually),
            new PropertyByName<Product>("Name", p => p.Name),
            new PropertyByName<Product>("ShortDescription", p => p.ShortDescription),
            new PropertyByName<Product>("FullDescription", p => p.FullDescription),
            new PropertyByName<Product>("Flag", p => p.Flag),
            new PropertyByName<Product>("BrandId", p => p.BrandId),
            new PropertyByName<Product>("VendorId", p => p.VendorId),
            new PropertyByName<Product>("ProductLayoutId", p => p.ProductLayoutId),
            new PropertyByName<Product>("ShowOnHomePage", p => p.ShowOnHomePage),
            new PropertyByName<Product>("BestSeller", p => p.BestSeller),
            new PropertyByName<Product>("MetaKeywords", p => p.MetaKeywords),
            new PropertyByName<Product>("MetaDescription", p => p.MetaDescription),
            new PropertyByName<Product>("MetaTitle", p => p.MetaTitle),
            new PropertyByName<Product>("SeName", p => p.GetSeName("")),
            new PropertyByName<Product>("AllowCustomerReviews", p => p.AllowCustomerReviews),
            new PropertyByName<Product>("Published", p => p.Published),
            new PropertyByName<Product>("SKU", p => p.Sku),
            new PropertyByName<Product>("Mpn", p => p.Mpn),
            new PropertyByName<Product>("Gtin", p => p.Gtin),
            new PropertyByName<Product>("IsGiftVoucher", p => p.IsGiftVoucher),
            new PropertyByName<Product>("GiftVoucherTypeId", p => (int)p.GiftVoucherTypeId),
            new PropertyByName<Product>("OverriddenGiftVoucherAmount", p => p.OverGiftAmount),
            new PropertyByName<Product>("RequireOtherProducts", p => p.RequireOtherProducts),
            new PropertyByName<Product>("RequiredProductIds", p => p.RequiredProductIds),
            new PropertyByName<Product>("AutomaticallyAddRequiredProducts", p => p.AutoAddRequiredProducts),
            new PropertyByName<Product>("IsDownload", p => p.IsDownload),
            new PropertyByName<Product>("DownloadId", p => p.DownloadId),
            new PropertyByName<Product>("UnlimitedDownloads", p => p.UnlimitedDownloads),
            new PropertyByName<Product>("MaxNumberOfDownloads", p => p.MaxNumberOfDownloads),
            new PropertyByName<Product>("DownloadActivationTypeId", p => (int)p.DownloadActivationTypeId),
            new PropertyByName<Product>("HasSampleDownload", p => p.HasSampleDownload),
            new PropertyByName<Product>("SampleDownloadId", p => p.SampleDownloadId),
            new PropertyByName<Product>("HasUserAgreement", p => p.HasUserAgreement),
            new PropertyByName<Product>("UserAgreementText", p => p.UserAgreementText),
            new PropertyByName<Product>("IsRecurring", p => p.IsRecurring),
            new PropertyByName<Product>("RecurringCycleLength", p => p.RecurringCycleLength),
            new PropertyByName<Product>("RecurringCyclePeriodId", p => (int)p.RecurringCyclePeriodId),
            new PropertyByName<Product>("RecurringTotalCycles", p => p.RecurringTotalCycles),
            new PropertyByName<Product>("Interval", p => p.Interval),
            new PropertyByName<Product>("IntervalUnitId", p => (int)p.IntervalUnitId),
            new PropertyByName<Product>("IsShipEnabled", p => p.IsShipEnabled),
            new PropertyByName<Product>("IsFreeShipping", p => p.IsFreeShipping),
            new PropertyByName<Product>("ShipSeparately", p => p.ShipSeparately),
            new PropertyByName<Product>("AdditionalShippingCharge", p => p.AdditionalShippingCharge),
            new PropertyByName<Product>("DeliveryDateId", p => p.DeliveryDateId),
            new PropertyByName<Product>("IsTaxExempt", p => p.IsTaxExempt),
            new PropertyByName<Product>("TaxCategoryId", p => p.TaxCategoryId),
            new PropertyByName<Product>("IsTele", p => p.IsTele),
            new PropertyByName<Product>("ManageInventoryMethodId", p => (int)p.ManageInventoryMethodId),
            new PropertyByName<Product>("UseMultipleWarehouses", p => p.UseMultipleWarehouses),
            new PropertyByName<Product>("WarehouseId", p => p.WarehouseId),
            new PropertyByName<Product>("StockQuantity", p => p.StockQuantity),
            new PropertyByName<Product>("ReservedQuantity", p => p.ReservedQuantity),
            new PropertyByName<Product>("DisplayStockAvailability", p => p.StockAvailability),
            new PropertyByName<Product>("DisplayStockQuantity", p => p.DisplayStockQuantity),
            new PropertyByName<Product>("MinStockQuantity", p => p.MinStockQuantity),
            new PropertyByName<Product>("LowStockActivityId", p => (int)p.LowStockActivityId),
            new PropertyByName<Product>("NotifyAdminForQuantityBelow", p => p.NotifyAdminForQuantityBelow),
            new PropertyByName<Product>("BackorderModeId", p => (int)p.BackorderModeId),
            new PropertyByName<Product>("AllowOutOfStockSubscriptions", p => p.AllowOutOfStockSubscriptions),
            new PropertyByName<Product>("OrderMinimumQuantity", p => p.OrderMinimumQuantity),
            new PropertyByName<Product>("OrderMaximumQuantity", p => p.OrderMaximumQuantity),
            new PropertyByName<Product>("AllowedQuantities", p => p.AllowedQuantities),
            new PropertyByName<Product>("DisableBuyButton", p => p.DisableBuyButton),
            new PropertyByName<Product>("DisableWishlistButton", p => p.DisableWishlistButton),
            new PropertyByName<Product>("AvailableForPreOrder", p => p.AvailableForPreOrder),
            new PropertyByName<Product>("PreOrderDateTimeUtc", p => p.PreOrderDateTimeUtc),
            new PropertyByName<Product>("CallForPrice", p => p.CallForPrice),
            new PropertyByName<Product>("Price", p => p.Price),
            new PropertyByName<Product>("OldPrice", p => p.OldPrice),
            new PropertyByName<Product>("CatalogPrice", p => p.CatalogPrice),
            new PropertyByName<Product>("ProductCost", p => p.ProductCost),
            new PropertyByName<Product>("EnteredPrice", p => p.EnteredPrice),
            new PropertyByName<Product>("MinEnteredPrice", p => p.MinEnteredPrice),
            new PropertyByName<Product>("MaxEnteredPrice", p => p.MaxEnteredPrice),
            new PropertyByName<Product>("BasepriceEnabled", p => p.BasepriceEnabled),
            new PropertyByName<Product>("BasepriceAmount", p => p.BasepriceAmount),
            new PropertyByName<Product>("BasepriceUnitId", p => p.BasepriceUnitId),
            new PropertyByName<Product>("BasepriceBaseAmount", p => p.BasepriceBaseAmount),
            new PropertyByName<Product>("BasepriceBaseUnitId", p => p.BasepriceBaseUnitId),
            new PropertyByName<Product>("MarkAsNew", p => p.MarkAsNew),
            new PropertyByName<Product>("MarkAsNewStartDateTimeUtc", p => p.MarkAsNewStartDateTimeUtc),
            new PropertyByName<Product>("MarkAsNewEndDateTimeUtc", p => p.MarkAsNewEndDateTimeUtc),
            new PropertyByName<Product>("UnitId", p => p.UnitId),
            new PropertyByName<Product>("Weight", p => p.Weight),
            new PropertyByName<Product>("Length", p => p.Length),
            new PropertyByName<Product>("Width", p => p.Width),
            new PropertyByName<Product>("Height", p => p.Height),
            new PropertyByName<Product>("DisplayOrder", p => p.DisplayOrder),
            new PropertyByName<Product>("DisplayOrderCategory", p => p.DisplayOrderCategory),
            new PropertyByName<Product>("DisplayOrderBrand", p => p.DisplayOrderBrand),
            new PropertyByName<Product>("DisplayOrderCollection", p => p.DisplayOrderCollection),
            new PropertyByName<Product>("OnSale", p => p.OnSale),
            new PropertyByName<Product>("CategoryIds",
                p => string.Join(";", p.ProductCategories.Select(n => n.CategoryId).ToArray())),
            new PropertyByName<Product>("CollectionIds",
                p => string.Join(";", p.ProductCollections.Select(n => n.CollectionId).ToArray())),
            new PropertyByName<Product>("Picture1", p => GetPictures(p).Result[0]),
            new PropertyByName<Product>("Picture2", p => GetPictures(p).Result[1]),
            new PropertyByName<Product>("Picture3", p => GetPictures(p).Result[2])
        };
        return await Task.FromResult(properties);
    }

    private async Task<string[]> GetPictures(Product product)
    {
        string picture1 = null;
        string picture2 = null;
        string picture3 = null;
        var i = 0;
        foreach (var picture in product.ProductPictures.Take(3))
        {
            var pic = await _pictureService.GetPictureById(picture.PictureId);
            var pictureLocalPath = await _pictureService.GetThumbPhysicalPath(pic);
            switch (i)
            {
                case 0:
                    picture1 = pictureLocalPath;
                    break;
                case 1:
                    picture2 = pictureLocalPath;
                    break;
                case 2:
                    picture3 = pictureLocalPath;
                    break;
            }

            i++;
        }

        return [picture1, picture2, picture3];
    }
}