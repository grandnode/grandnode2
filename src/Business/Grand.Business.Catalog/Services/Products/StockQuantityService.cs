﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Catalog.Services.Products
{
    public class StockQuantityService : IStockQuantityService
    {
        private readonly ITranslationService _translationService;
        public StockQuantityService(
            ITranslationService translationService)
        {
            _translationService = translationService;
        }

        public virtual int GetTotalStockQuantity(Product product, bool useReservedQuantity = true,
            string warehouseId = "", bool total = false)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock)
            {
                return 0;
            }

            if (product.UseMultipleWarehouses)
            {
                if (total)
                {
                    return useReservedQuantity ? product.ProductWarehouseInventory.Sum(x => x.StockQuantity - x.ReservedQuantity) : product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
                }

                var pwi = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (pwi == null) return 0;
                var result = pwi.StockQuantity;
                if (useReservedQuantity)
                {
                    result -= pwi.ReservedQuantity;
                }
                return result;
            }
            if (string.IsNullOrEmpty(warehouseId) || string.IsNullOrEmpty(product.WarehouseId))
                return product.StockQuantity - (useReservedQuantity ? product.ReservedQuantity : 0);
            
            if (product.WarehouseId == warehouseId)
                return product.StockQuantity - (useReservedQuantity ? product.ReservedQuantity : 0);
            
            return 0;
        }

        public virtual int GetTotalStockQuantityForCombination(Product product, ProductAttributeCombination combination,
            bool useReservedQuantity = true, string warehouseId = "")
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (combination == null)
                throw new ArgumentNullException(nameof(combination));

            if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
            {
                return 0;
            }

            if (product.UseMultipleWarehouses)
            {
                var pwi = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (pwi == null) return 0;
                var result = pwi.StockQuantity;
                if (useReservedQuantity)
                {
                    result -= pwi.ReservedQuantity;
                }
                return result;
            }

            if (string.IsNullOrEmpty(warehouseId) || string.IsNullOrEmpty(product.WarehouseId))
                return combination.StockQuantity - (useReservedQuantity ? combination.ReservedQuantity : 0);
            
            if (product.WarehouseId == warehouseId)
                return combination.StockQuantity - (useReservedQuantity ? combination.ReservedQuantity : 0);
            
            return 0;
        }

        public virtual string FormatStockMessage(Product product, string warehouseId, IList<CustomAttribute> attributes)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var stockMessage = string.Empty;

            switch (product.ManageInventoryMethodId)
            {
                case ManageInventoryMethod.ManageStock:
                    {
                        #region Manage stock

                        if (!product.StockAvailability)
                            return stockMessage;

                        var stockQuantity = GetTotalStockQuantity(product, warehouseId: warehouseId);
                        if (stockQuantity > 0)
                        {
                            stockMessage = product.DisplayStockQuantity ?
                                string.Format(_translationService.GetResource("Products.Availability.InStockWithQuantity"), stockQuantity) :
                                _translationService.GetResource("Products.Availability.InStock");
                        }
                        else
                        {
                            switch (product.BackorderModeId)
                            {
                                case BackorderMode.NoBackorders:
                                    stockMessage = _translationService.GetResource("Products.Availability.OutOfStock");
                                    break;
                                case BackorderMode.AllowQtyBelowZero:
                                    stockMessage = _translationService.GetResource("Products.Availability.Backordering");
                                    break;
                                default:
                                    break;
                            }
                        }

                        #endregion
                    }
                    break;
                case ManageInventoryMethod.ManageStockByAttributes:
                    {
                        #region Manage stock by attributes

                        if (!product.StockAvailability)
                            return stockMessage;

                        var combination = product.FindProductAttributeCombination(attributes);
                        if (combination != null)
                        {
                            //combination exists
                            var stockQuantity = GetTotalStockQuantityForCombination(product, combination, warehouseId: warehouseId);
                            if (stockQuantity > 0)
                            {
                                stockMessage = product.DisplayStockQuantity ?
                                    //display "in stock" with stock quantity
                                    string.Format(_translationService.GetResource("Products.Availability.InStockWithQuantity"), stockQuantity) :
                                    //display "in stock" without stock quantity
                                    _translationService.GetResource("Products.Availability.InStock");
                            }
                            else
                            {
                                //out of stock
                                switch (product.BackorderModeId)
                                {
                                    case BackorderMode.NoBackorders:
                                        stockMessage = _translationService.GetResource("Products.Availability.Attributes.OutOfStock");
                                        break;
                                    case BackorderMode.AllowQtyBelowZero:
                                        stockMessage = _translationService.GetResource("Products.Availability.Attributes.Backordering");
                                        break;
                                    default:
                                        break;
                                }
                                if (!combination.AllowOutOfStockOrders)
                                    stockMessage = _translationService.GetResource("Products.Availability.Attributes.OutOfStock");
                            }
                        }
                        else
                        {
                            stockMessage = _translationService.GetResource("Products.Availability.AttributeCombinationsNotExists");
                        }

                        #endregion
                    }
                    break;
                case ManageInventoryMethod.DontManageStock:
                case ManageInventoryMethod.ManageStockByBundleProducts:
                default:
                    return stockMessage;
            }
            return stockMessage;
        }

    }
}
