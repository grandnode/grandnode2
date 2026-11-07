using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using System.Net;
using ProductExtensions = Grand.Domain.Catalog.ProductExtensions;

namespace Grand.Web.AdminShared.Services;

public class ProductAttributeCombinationViewModelService(
    IPictureService pictureService,
    IProductAttributeService productAttributeService,
    ICurrencyService currencyService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    IGroupService groupService,
    IWarehouseService warehouseService,
    IStoreService storeService,
    IProductAttributeFormatter productAttributeFormatter,
    CurrencySettings currencySettings,
    IStockMovementService stockMovementService,
    ISettingService settingService)
    : IProductAttributeCombinationViewModelService
{
    public virtual async Task PrepareAddProductAttributeCombinationModel(ProductAttributeCombinationModel model,
        Product product)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(product);

        if (product.UseMultipleWarehouses) model.UseMultipleWarehouses = product.UseMultipleWarehouses;
        if (string.IsNullOrEmpty(model.Id))
        {
            model.ProductId = product.Id;
            var attributes = product.ProductAttributeMappings
                .Where(x => !x.IsNonCombinable())
                .ToList();
            foreach (var attribute in attributes)
            {
                var productAttribute =
                    await productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
                var attributeModel = new ProductAttributeCombinationModel.ProductAttributeModel {
                    Id = attribute.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = productAttribute.Name,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlTypeId
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.ProductAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new ProductAttributeCombinationModel.ProductAttributeValueModel {
                            Id = attributeValue.Id,
                            Name = attributeValue.Name,
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(attributeValueModel);
                    }
                }

                model.ProductAttributes.Add(attributeModel);
            }
        }


        if (!string.IsNullOrEmpty(model.PictureId))
        {
            var pictureThumbnailUrl = await pictureService.GetPictureUrl(model.PictureId, 100, false);
            model.PictureThumbnailUrl = pictureThumbnailUrl;
        }

        foreach (var picture in product.ProductPictures)
            model.ProductPictureModels.Add(new ProductModel.ProductPictureModel {
                Id = picture.Id,
                ProductId = product.Id,
                PictureId = picture.PictureId,
                PictureUrl = await pictureService.GetPictureUrl(picture.PictureId),
                DisplayOrder = picture.DisplayOrder,
                IsDefault = picture.IsDefault
            });
        model.PrimaryStoreCurrencyCode =
            (await currencyService.GetCurrencyById(currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode;
    }

    public virtual async Task<IList<ProductModel.ProductAttributeCombinationModel>>
        PrepareProductAttributeCombinationModel(Product product)
    {
        var items = new List<ProductModel.ProductAttributeCombinationModel>();
        foreach (var x in product.ProductAttributeCombinations)
        {
            var attributes = await productAttributeFormatter.FormatAttributes(product, x.Attributes,
                contextAccessor.CalculationContext(), contextAccessor.WorkContext.WorkingLanguage,
                contextAccessor.StoreContext.CurrentHost, "<br />", true, true, true, false, true, true);
            var pacModel = new ProductModel.ProductAttributeCombinationModel {
                Id = x.Id,
                ProductId = product.Id,
                Attributes = string.IsNullOrEmpty(attributes) ? "(null)" : attributes,
                StockQuantity = product.UseMultipleWarehouses
                    ? x.WarehouseInventory.Sum(y => y.StockQuantity - y.ReservedQuantity)
                    : x.StockQuantity,
                AllowOutOfStockOrders = x.AllowOutOfStockOrders,
                Sku = x.Sku,
                Mpn = x.Mpn,
                Gtin = x.Gtin,
                OverriddenPrice = x.OverriddenPrice,
                NotifyAdminForQuantityBelow = x.NotifyAdminForQuantityBelow
            };
            items.Add(pacModel);
        }

        return items;
    }

    public virtual async Task<ProductAttributeCombinationModel> PrepareProductAttributeCombinationModel(Product product,
        string combinationId)
    {
        var model = new ProductAttributeCombinationModel();
        var stockChangesOnlyThroughDocuments =
            (await settingService.LoadSetting<InventorySettings>()).StockChangesOnlyThroughDocuments;
        var wim = new List<ProductAttributeCombinationModel.WarehouseInventoryModel>();
        foreach (var warehouse in await warehouseService.GetAllWarehouses())
        {
            var pwiModel = new ProductAttributeCombinationModel.WarehouseInventoryModel {
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name
            };
            wim.Add(pwiModel);
        }

        if (product.UseMultipleWarehouses)
        {
            model.UseMultipleWarehouses = product.UseMultipleWarehouses;
            model.WarehouseInventoryModels = wim;
        }

        if (!string.IsNullOrEmpty(combinationId))
        {
            var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == combinationId);
            if (combination != null)
            {
                model = combination.ToModel();
                //the stock the form shows: only a value changed since is applied when it is saved
                model.OriginalStockQuantity = combination.StockQuantity;
                model.OriginalReservedQuantity = combination.ReservedQuantity;
                model.UseMultipleWarehouses = product.UseMultipleWarehouses;
                model.WarehouseInventoryModels = wim;
                model.ProductId = product.Id;
                model.Attributes = await productAttributeFormatter.FormatAttributes(product, combination.Attributes,
                    contextAccessor.CalculationContext(), contextAccessor.WorkContext.WorkingLanguage,
                    contextAccessor.StoreContext.CurrentHost, "<br />", true, true, true, false);
                if (model.UseMultipleWarehouses)
                    foreach (var _winv in combination.WarehouseInventory)
                    {
                        var warehouseInventoryModel =
                            model.WarehouseInventoryModels.FirstOrDefault(x => x.WarehouseId == _winv.WarehouseId);
                        if (warehouseInventoryModel != null)
                        {
                            warehouseInventoryModel.WarehouseUsed = true;
                            warehouseInventoryModel.Id = _winv.Id;
                            warehouseInventoryModel.StockQuantity = _winv.StockQuantity;
                            warehouseInventoryModel.ReservedQuantity = _winv.ReservedQuantity;
                            warehouseInventoryModel.OriginalStockQuantity = _winv.StockQuantity;
                            warehouseInventoryModel.OriginalReservedQuantity = _winv.ReservedQuantity;
                        }
                    }
            }
        }

        model.StockChangesOnlyThroughDocuments = stockChangesOnlyThroughDocuments;
        return model;
    }

    public virtual async Task<IList<string>> InsertOrUpdateProductAttributeCombinationPopup(Product product,
        ProductAttributeCombinationModel model)
    {
        var customAttributes = new List<CustomAttribute>();
        var warnings = new List<string>();

        var source = new StockSource(nameof(Product), product.Id, Reference: product.Sku ?? "",
            CustomerId: contextAccessor.WorkContext.CurrentCustomer.Id);

        //only the values changed since the popup was shown; an unticked warehouse gives up its stock
        List<StockChange> StockChanges(ProductAttributeCombination combination)
        {
            if (!product.UseMultipleWarehouses)
                return new[] {
                    StockFormChange.Of(combination.Attributes, "", model.StockQuantity, model.OriginalStockQuantity,
                        model.ReservedQuantity, model.OriginalReservedQuantity, source)
                }.Where(x => x != null).ToList();

            return model.WarehouseInventoryModels
                .Select(x => StockFormChange.Of(combination.Attributes, x.WarehouseId,
                    x.WarehouseUsed ? x.StockQuantity : 0, x.OriginalStockQuantity,
                    x.WarehouseUsed ? x.ReservedQuantity : 0, x.OriginalReservedQuantity, source))
                .Where(x => x != null).ToList();
        }

        //the server is the guard, not the read-only fields
        async Task<bool> Refused(List<StockChange> changes)
        {
            if (changes.Count == 0)
                return false;

            //a combination holds stock only for a product stocked by attributes
            if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
                warnings.Add(translationService.GetResource(
                    "Admin.Catalog.Products.ProductAttributeCombinations.StockNotTracked"));
            else if (changes.Any(x => x.SetTo) &&
                     (await settingService.LoadSetting<InventorySettings>()).StockChangesOnlyThroughDocuments)
                warnings.Add(translationService.GetResource("Admin.Catalog.Products.StockChangesOnlyThroughDocuments"));

            return warnings.Count > 0;
        }

        async Task ApplyStock(List<StockChange> changes)
        {
            if (changes.Count > 0)
                warnings.AddRange((await stockMovementService.Apply(product.Id, changes))
                    .Select(translationService.GetResource));
        }

        void MarkUsedWarehouses(ProductAttributeCombination combination)
        {
            combination.WarehouseInventory.Clear();
            foreach (var whim in model.WarehouseInventoryModels.Where(x => x.WarehouseUsed))
                combination.WarehouseInventory.Add(new ProductCombinationWarehouseInventory
                    { WarehouseId = whim.WarehouseId });
        }

        if (string.IsNullOrEmpty(model.Id))
        {
            #region Product attributes

            var attributes = product.ProductAttributeMappings
                .Where(x => !x.IsNonCombinable())
                .ToList();
            if (attributes.Count == 0)
            {
                warnings.Add("This combination attributes is empty!");
                return warnings;
            }

            foreach (var attribute in attributes)
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            var ctrlAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                                customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                    attribute, ctrlAttributes).ToList();
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var cblAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                            if (!string.IsNullOrEmpty(cblAttributes))
                                foreach (var item in cblAttributes.Split([','],
                                             StringSplitOptions.RemoveEmptyEntries))
                                    if (!string.IsNullOrEmpty(item))
                                        customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                            attribute, item).ToList();
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = attribute.ProductAttributeValues;
                            foreach (var selectedAttributeId in attributeValues
                                         .Where(v => v.IsPreSelected)
                                         .Select(v => v.Id)
                                         .ToList())
                                customAttributes = ProductExtensions.AddProductAttribute(customAttributes,
                                    attribute, selectedAttributeId).ToList();
                        }
                        break;
                }

            //validate conditional attributes (if specified)
            foreach (var attribute in attributes)
            {
                var conditionMet = product.IsConditionMet(attribute, customAttributes);
                if (conditionMet.HasValue && !conditionMet.Value)
                    customAttributes = ProductExtensions.RemoveProductAttribute(customAttributes, attribute).ToList();
            }

            if (customAttributes.Count == 0)
            {
                warnings.Add("This combination custom attributes is empty!");
                return warnings;
            }

            foreach (var customAttribute in customAttributes)
                if (string.IsNullOrEmpty(customAttribute.Value))
                {
                    warnings.Add("Combination custom attributes need to be selected value!");
                    return warnings;
                }

            #endregion

            if (product.FindProductAttributeCombination(customAttributes) != null)
                warnings.Add("This combination attributes exists!");
            if (warnings.Count == 0)
            {
                var combination = new ProductAttributeCombination {
                    Attributes = customAttributes,
                    AllowOutOfStockOrders = model.AllowOutOfStockOrders,
                    Sku = model.Sku,
                    Text = model.Text,
                    Mpn = model.Mpn,
                    Gtin = model.Gtin,
                    OverriddenPrice = model.OverriddenPrice,
                    NotifyAdminForQuantityBelow = model.NotifyAdminForQuantityBelow,
                    PictureId = model.PictureId
                };

                var changes = StockChanges(combination);
                if (await Refused(changes))
                    return warnings;

                if (product.UseMultipleWarehouses)
                    MarkUsedWarehouses(combination);

                await productAttributeService.InsertProductAttributeCombination(combination, product.Id);
                product.ProductAttributeCombinations.Add(combination);

                await ApplyStock(changes);
            }
        }
        else
        {
            var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == model.Id);
            if (combination == null)
            {
                warnings.Add("This combination doesn't exist!");
                return warnings;
            }

            combination.AllowOutOfStockOrders = model.AllowOutOfStockOrders;
            combination.Sku = model.Sku;
            combination.Text = model.Text;
            combination.Mpn = model.Mpn;
            combination.Gtin = model.Gtin;
            combination.OverriddenPrice = model.OverriddenPrice;
            combination.NotifyAdminForQuantityBelow = model.NotifyAdminForQuantityBelow;
            combination.PictureId = model.PictureId;

            //the stock first: a warehouse left unticked is emptied, then the update drops its row
            var changes = StockChanges(combination);
            if (await Refused(changes))
                return warnings;

            await ApplyStock(changes);
            if (warnings.Count > 0)
                return warnings;

            if (product.UseMultipleWarehouses)
                MarkUsedWarehouses(combination);

            //update combination
            await productAttributeService.UpdateProductAttributeCombination(combination, product.Id);
        }

        return warnings;
    }

    public virtual async Task GenerateAllAttributeCombinations(Product product)
    {
        var allAttributesComb = product.GenerateAllCombinations();
        if (allAttributesComb == null || allAttributesComb.Count == 0)
            return;

        foreach (var attributes in allAttributesComb)
        {
            var customAttributes = attributes.ToList();
            if (!customAttributes.Any())
                continue;

            var existingCombination = product.FindProductAttributeCombination(customAttributes);

            //already exists?
            if (existingCombination != null)
                continue;

            //save combination
            var combination = new ProductAttributeCombination {
                Attributes = customAttributes.ToList(),
                StockQuantity = 0,
                AllowOutOfStockOrders = false,
                Sku = null,
                Mpn = null,
                Gtin = null,
                OverriddenPrice = null,
                NotifyAdminForQuantityBelow = 1
            };
            await productAttributeService.InsertProductAttributeCombination(combination, product.Id);
            product.ProductAttributeCombinations.Add(combination);
        }

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            await stockMovementService.RecomputeSums(product.Id);
    }

    public virtual async Task ClearAllAttributeCombinations(Product product)
    {
        foreach (var combination in product.ProductAttributeCombinations)
            await productAttributeService.DeleteProductAttributeCombination(combination, product.Id);
    }

    public virtual async Task<IList<ProductModel.ProductAttributeCombinationTierPricesModel>>
        PrepareProductAttributeCombinationTierPricesModel(Product product, string productAttributeCombinationId)
    {
        var items = new List<ProductModel.ProductAttributeCombinationTierPricesModel>();
        foreach (var x in product.ProductAttributeCombinations.Where(x => x.Id == productAttributeCombinationId)
                     .SelectMany(x => x.TierPrices))
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

            var priceModel = new ProductModel.ProductAttributeCombinationTierPricesModel {
                Id = x.Id,
                CustomerGroupId = x.CustomerGroupId,
                CustomerGroup = !string.IsNullOrEmpty(x.CustomerGroupId)
                    ? (await groupService.GetCustomerGroupById(x.CustomerGroupId)).Name
                    : translationService.GetResource("Admin.Catalog.Products.TierPrices.Fields.CustomerGroup.All"),
                StoreId = x.StoreId,
                Store = storeName,
                Price = x.Price,
                Quantity = x.Quantity
            };
            items.Add(priceModel);
        }

        return items;
    }

    public virtual async Task InsertProductAttributeCombinationTierPricesModel(Product product,
        ProductAttributeCombination productAttributeCombination,
        ProductModel.ProductAttributeCombinationTierPricesModel model)
    {
        model.CustomerGroupId = !string.IsNullOrEmpty(model.CustomerGroupId) ? model.CustomerGroupId.Trim() : "";

        model.StoreId = !string.IsNullOrEmpty(model.StoreId) ? model.StoreId.Trim() : "";

        if (productAttributeCombination != null)
        {
            var pctp = new ProductCombinationTierPrices {
                Price = model.Price,
                Quantity = model.Quantity,
                StoreId = model.StoreId,
                CustomerGroupId = model.CustomerGroupId
            };
            productAttributeCombination.TierPrices.Add(pctp);
            await productAttributeService.UpdateProductAttributeCombination(productAttributeCombination, product.Id);
        }
    }

    public virtual async Task UpdateProductAttributeCombinationTierPricesModel(Product product,
        ProductAttributeCombination productAttributeCombination,
        ProductModel.ProductAttributeCombinationTierPricesModel model)
    {
        model.CustomerGroupId = !string.IsNullOrEmpty(model.CustomerGroupId) ? model.CustomerGroupId.Trim() : "";
        model.StoreId = !string.IsNullOrEmpty(model.StoreId) ? model.StoreId.Trim() : "";

        if (productAttributeCombination != null)
        {
            var tierPrice = productAttributeCombination.TierPrices.FirstOrDefault(x => x.Id == model.Id);
            if (tierPrice != null)
            {
                tierPrice.Price = model.Price;
                tierPrice.Quantity = model.Quantity;
                tierPrice.StoreId = model.StoreId;
                tierPrice.CustomerGroupId = model.CustomerGroupId;
                await productAttributeService.UpdateProductAttributeCombination(productAttributeCombination,
                    product.Id);
            }
        }
    }

    public virtual async Task DeleteProductAttributeCombinationTierPrices(Product product,
        ProductAttributeCombination productAttributeCombination, ProductCombinationTierPrices tierPrice)
    {
        productAttributeCombination.TierPrices.Remove(tierPrice);
        await productAttributeService.UpdateProductAttributeCombination(productAttributeCombination, product.Id);
    }
}
