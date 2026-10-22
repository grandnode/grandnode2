using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Storage;
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
    IProductService productService,
    IInventoryManageService inventoryManageService,
    IPictureService pictureService,
    IProductAttributeService productAttributeService,
    ICurrencyService currencyService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    IGroupService groupService,
    IWarehouseService warehouseService,
    IStoreService storeService,
    IOutOfStockSubscriptionService outOfStockSubscriptionService,
    IProductAttributeFormatter productAttributeFormatter,
    CurrencySettings currencySettings)
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

    public virtual async Task OutOfStockNotifications(Product product, ProductAttributeCombination combination,
        ProductAttributeCombination prevcombination)
    {
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes &&
            product.BackorderModeId == BackorderMode.NoBackorders &&
            product.AllowOutOfStockSubscriptions &&
            combination.StockQuantity > 0 &&
            prevcombination.StockQuantity <= 0 && !product.UseMultipleWarehouses &&
            product.Published)
            await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, combination.Attributes, "");
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes &&
            product.BackorderModeId == BackorderMode.NoBackorders &&
            product.AllowOutOfStockSubscriptions &&
            product.UseMultipleWarehouses &&
            product.Published)
        {
            foreach (var prevstock in prevcombination.WarehouseInventory)
                if (prevstock.StockQuantity - prevstock.ReservedQuantity <= 0)
                {
                    var actualStock =
                        combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == prevstock.WarehouseId);
                    if (actualStock != null)
                        if (actualStock.StockQuantity - actualStock.ReservedQuantity > 0)
                            await outOfStockSubscriptionService.SendNotificationsToSubscribers(product,
                                combination.Attributes, prevstock.WarehouseId);
                }

            if (combination.WarehouseInventory.Sum(x => x.StockQuantity - x.ReservedQuantity) > 0)
                if (prevcombination.WarehouseInventory.Sum(x => x.StockQuantity - x.ReservedQuantity) <= 0)
                    await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, combination.Attributes,
                        "");
        }
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
                        }
                    }
            }
        }

        return model;
    }

    public virtual async Task<IList<string>> InsertOrUpdateProductAttributeCombinationPopup(Product product,
        ProductAttributeCombinationModel model)
    {
        var customAttributes = new List<CustomAttribute>();
        var warnings = new List<string>();

        async Task PrepareCombinationWarehouseInventory(ProductAttributeCombination combination)
        {
            var warehouses = await warehouseService.GetAllWarehouses();

            foreach (var warehouse in warehouses)
            {
                var whim = model.WarehouseInventoryModels.FirstOrDefault(x => x.WarehouseId == warehouse.Id);
                var existingPwI = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouse.Id);
                if (existingPwI != null)
                {
                    if (whim is { WarehouseUsed: true })
                    {
                        //update 
                        existingPwI.StockQuantity = whim.StockQuantity;
                        existingPwI.ReservedQuantity = whim.ReservedQuantity;
                    }
                    else
                    {
                        //delete 
                        combination.WarehouseInventory.Remove(existingPwI);
                    }
                }
                else
                {
                    if (whim is { WarehouseUsed: true })
                    {
                        //no need to insert a record for qty 0
                        existingPwI = new ProductCombinationWarehouseInventory {
                            WarehouseId = whim.WarehouseId,
                            StockQuantity = whim.StockQuantity,
                            ReservedQuantity = whim.ReservedQuantity
                        };
                        combination.WarehouseInventory.Add(existingPwI);
                    }
                }
            }
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
                    StockQuantity = model.StockQuantity,
                    ReservedQuantity = model.ReservedQuantity,
                    AllowOutOfStockOrders = model.AllowOutOfStockOrders,
                    Sku = model.Sku,
                    Text = model.Text,
                    Mpn = model.Mpn,
                    Gtin = model.Gtin,
                    OverriddenPrice = model.OverriddenPrice,
                    NotifyAdminForQuantityBelow = model.NotifyAdminForQuantityBelow,
                    PictureId = model.PictureId
                };

                if (product.UseMultipleWarehouses)
                {
                    await PrepareCombinationWarehouseInventory(combination);
                    combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
                    combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);
                }

                await productAttributeService.InsertProductAttributeCombination(combination, product.Id);

                if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
                {
                    product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
                    product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
                    await inventoryManageService.UpdateStockProduct(product, false);
                }
            }
        }
        else
        {
            var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == model.Id);
            var prevCombination = (ProductAttributeCombination)combination!.Clone();

            combination.StockQuantity = model.StockQuantity;
            combination.ReservedQuantity = model.ReservedQuantity;
            combination.AllowOutOfStockOrders = model.AllowOutOfStockOrders;
            combination.Sku = model.Sku;
            combination.Text = model.Text;
            combination.Mpn = model.Mpn;
            combination.Gtin = model.Gtin;
            combination.OverriddenPrice = model.OverriddenPrice;
            combination.NotifyAdminForQuantityBelow = model.NotifyAdminForQuantityBelow;
            combination.PictureId = model.PictureId;

            if (product.UseMultipleWarehouses)
            {
                await PrepareCombinationWarehouseInventory(combination);
                combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
            }

            //notification - out of stock
            await OutOfStockNotifications(product, combination, prevCombination);

            //update combination
            await productAttributeService.UpdateProductAttributeCombination(combination, product.Id);

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                var pr = await productService.GetProductById(model.ProductId);
                pr.StockQuantity = pr.ProductAttributeCombinations.Sum(x => x.StockQuantity);
                pr.ReservedQuantity = pr.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
                await inventoryManageService.UpdateStockProduct(pr, false);
            }
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
        }

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
        {
            product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
            product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
            await inventoryManageService.UpdateStockProduct(product, false);
        }
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
