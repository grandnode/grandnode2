using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, bool>
{
    private readonly IContextAccessor _contextAccessor;
    private readonly IInventoryManageService _inventoryManageService;
    private readonly IProductService _productService;
    private readonly IStockMovementService _stockMovementService;

    public UpdateProductStockCommandHandler(
        IProductService productService,
        IInventoryManageService inventoryManageService,
        IStockMovementService stockMovementService,
        IContextAccessor contextAccessor)
    {
        _productService = productService;
        _inventoryManageService = inventoryManageService;
        _stockMovementService = stockMovementService;
        _contextAccessor = contextAccessor;
    }

    public async Task<bool> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Product.Id);
        if (product == null)
            return true;

        //the stock of a product tracking batches or serial numbers changes only with the units named
        if (InventoryTracking.Tracked(product))
            return false;

        if (!string.IsNullOrEmpty(request.WarehouseId) && !product.UseMultipleWarehouses)
            throw new ArgumentException(
                "Product don't support multiple warehouses (warehouseId should be null or empty)");

        //a product that tracks no stock keeps the value as a plain counter
        if (product.ManageInventoryMethodId is not (ManageInventoryMethod.ManageStock
            or ManageInventoryMethod.ManageStockByAttributes))
        {
            product.StockQuantity = request.Stock;
            await _inventoryManageService.UpdateStockProduct(product, false);
            return true;
        }

        var errors = await _stockMovementService.Apply(product.Id, [
            new StockChange([], request.WarehouseId ?? "", request.Stock, true, InventoryMovementType.Api,
                new StockSource("Api", product.Id, Reference: product.Sku ?? "",
                    CustomerId: _contextAccessor.WorkContext?.CurrentCustomer?.Id ?? ""))
        ]);
        if (errors.Count > 0)
            throw new ArgumentException(errors[0]);

        return true;
    }
}
