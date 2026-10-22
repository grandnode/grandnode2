using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Catalog;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;

namespace Grand.Web.AdminShared.Services;

public class ProductPriceViewModelService(
    ICurrencyService currencyService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IGroupService groupService,
    IStoreService storeService,
    IAdminDataScope<Product> scope)
    : IProductPriceViewModelService
{
    public virtual async Task PrepareTierPriceModel(ProductModel.TierPriceModel model)
    {
        var storeId = scope.DefaultStoreId ?? "";

        //stores - same capability gate as PrepareProductListModel: Vendor's original PrepareTierPriceModel
        //never populated a stores dropdown at all (vendors don't pick stores), so this must not run for
        //Vendor even though its storeId also resolves to "".
        if (scope.ShowStoreSelector)
        {
            if (string.IsNullOrEmpty(storeId))
                model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });

            foreach (var store in (await storeService.GetAllStores()).Where(x =>
                         x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id });
        }

        //customer groups
        model.AvailableCustomerGroups.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var role in await groupService.GetAllCustomerGroups(showHidden: true))
            model.AvailableCustomerGroups.Add(new SelectListItem { Text = role.Name, Value = role.Id });

        foreach (var currency in await currencyService.GetAllCurrencies())
            model.AvailableCurrencies.Add(new SelectListItem { Text = currency.Name, Value = currency.CurrencyCode });
    }

    public virtual async Task<IList<ProductModel.TierPriceModel>> PrepareTierPriceModel(Product product)
    {
        var storeId = scope.DefaultStoreId ?? "";
        var items = new List<ProductModel.TierPriceModel>();
        foreach (var x in product.TierPrices
                     .Where(x => x.StoreId == storeId || string.IsNullOrWhiteSpace(storeId) ||
                                 string.IsNullOrWhiteSpace(x.StoreId)).OrderBy(x => x.StoreId).ThenBy(x => x.Quantity)
                     .ThenBy(x => x.CustomerGroupId))
        {
            string storeName;
            if (!string.IsNullOrEmpty(x.StoreId))
            {
                var store = await storeService.GetStoreById(x.StoreId);
                storeName = store != null ? store.Shortcut : "Deleted";
            }
            else
            {
                storeName = translationService.GetResource("Admin.Catalog.Products.TierPrices.Fields.Store.All");
            }

            var groupName = string.Empty;
            if (string.IsNullOrEmpty(x.CustomerGroupId))
                groupName = translationService.GetResource(
                    "Admin.Catalog.Products.TierPrices.Fields.CustomerGroup.All");
            else
                groupName = (await groupService.GetCustomerGroupById(x.CustomerGroupId))?.Name ?? "NOT FOUND";

            items.Add(new ProductModel.TierPriceModel {
                Id = x.Id,
                StoreId = x.StoreId,
                Store = storeName,
                CurrencyCode = x.CurrencyCode,
                CustomerGroup = groupName,
                ProductId = product.Id,
                CustomerGroupId = !string.IsNullOrEmpty(x.CustomerGroupId) ? x.CustomerGroupId : "",
                Quantity = x.Quantity,
                Price = x.Price,
                StartDateTime = x.StartDateTimeUtc.HasValue
                    ? dateTimeService.ConvertToUserTime(x.StartDateTimeUtc.Value, DateTimeKind.Utc)
                    : new DateTime?(),
                EndDateTime = x.EndDateTimeUtc.HasValue
                    ? dateTimeService.ConvertToUserTime(x.EndDateTimeUtc.Value, DateTimeKind.Utc)
                    : new DateTime?()
            });
        }

        return items;
    }
}
