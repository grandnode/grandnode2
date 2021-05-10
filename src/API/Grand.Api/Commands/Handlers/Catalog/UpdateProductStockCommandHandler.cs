using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain.Catalog;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, bool>
    {
        private readonly IProductService _productService;
        private readonly IInventoryManageService _inventoryManageService;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;

        public UpdateProductStockCommandHandler(
            IProductService productService,
            IInventoryManageService inventoryManageService,
            IStockQuantityService stockQuantityService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IOutOfStockSubscriptionService outOfStockSubscriptionService)
        {
            _productService = productService;
            _inventoryManageService = inventoryManageService;
            _stockQuantityService = stockQuantityService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _outOfStockSubscriptionService = outOfStockSubscriptionService;
        }

        public async Task<bool> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductById(request.Product.Id);
            if (product != null)
            {
                var prevStockQuantity = _stockQuantityService.GetTotalStockQuantity(product);
                var prevMultiWarehouseStock = product.ProductWarehouseInventory.Select(i => new ProductWarehouseInventory() { WarehouseId = i.WarehouseId, StockQuantity = i.StockQuantity, ReservedQuantity = i.ReservedQuantity }).ToList();

                if (string.IsNullOrEmpty(request.WarehouseId))
                {
                    product.StockQuantity = request.Stock;
                    await _inventoryManageService.UpdateStockProduct(product, false);
                }
                else
                {
                    if (product.UseMultipleWarehouses)
                    {
                        var existingPwI = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == request.WarehouseId);
                        if (existingPwI != null)
                        {
                            existingPwI.StockQuantity = request.Stock;
                            await _productService.UpdateProductWarehouseInventory(existingPwI, product.Id);
                        }
                        else
                        {
                            var newPwI = new ProductWarehouseInventory
                            {
                                WarehouseId = request.WarehouseId,
                                StockQuantity = request.Stock,
                                ReservedQuantity = 0
                            };
                            await _productService.InsertProductWarehouseInventory(newPwI, product.Id);
                        }

                        product.StockQuantity = product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
                        product.ReservedQuantity = product.ProductWarehouseInventory.Sum(x => x.ReservedQuantity);
                        await _inventoryManageService.UpdateStockProduct(product, false);
                    }
                    else
                    {
                        throw new ArgumentException("Product don't support multiple warehouses (warehouseId should be null or empty)");
                    }
                }
                await OutOfStockNotifications(product, prevStockQuantity, prevMultiWarehouseStock);

                //activity log
                await _customerActivityService.InsertActivity("EditProduct", product.Id, _translationService.GetResource("ActivityLog.EditProduct"), product.Name);

            }
            return true;
        }

        protected async Task OutOfStockNotifications(Product product, int prevStockQuantity, List<ProductWarehouseInventory> prevMultiWarehouseStock)
        {
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
                product.BackorderModeId == BackorderMode.NoBackorders &&
                product.AllowOutOfStockSubscriptions &&
                _stockQuantityService.GetTotalStockQuantity(product) > 0 &&
                prevStockQuantity <= 0 && !product.UseMultipleWarehouses &&
                product.Published)
            {
                await _outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");
            }
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
                product.BackorderModeId == BackorderMode.NoBackorders &&
                product.AllowOutOfStockSubscriptions &&
                product.UseMultipleWarehouses &&
                product.Published)
            {
                foreach (var prevstock in prevMultiWarehouseStock)
                {
                    if (prevstock.StockQuantity - prevstock.ReservedQuantity <= 0)
                    {
                        var actualStock = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == prevstock.WarehouseId);
                        if (actualStock != null)
                        {
                            if (actualStock.StockQuantity - actualStock.ReservedQuantity > 0)
                                await _outOfStockSubscriptionService.SendNotificationsToSubscribers(product, prevstock.WarehouseId);
                        }
                    }
                }
                if (product.ProductWarehouseInventory.Sum(x => x.StockQuantity - x.ReservedQuantity) > 0)
                {
                    if (prevMultiWarehouseStock.Sum(x => x.StockQuantity - x.ReservedQuantity) <= 0)
                    {
                        await _outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");
                    }
                }
            }
        }



    }
}
