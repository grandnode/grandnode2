using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IContextAccessor _contextAccessor;
    private readonly IMediator _mediator;
    private readonly IProductService _productService;
    private readonly ISlugService _slugService;
    private readonly IStockMovementService _stockMovementService;
    private readonly ISeNameService _seNameService;
    public UpdateProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        IStockMovementService stockMovementService,
        IContextAccessor contextAccessor,
        IMediator mediator,
        ISeNameService seNameService)
    {
        _productService = productService;
        _slugService = slugService;
        _stockMovementService = stockMovementService;
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _seNameService = seNameService;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        //product
        var product = await _productService.GetProductById(request.Model.Id);
        var prevStockQuantity = product.StockQuantity;
        var prevReservedQuantity = product.ReservedQuantity;
        var prevPublished = product.Published;

        product = request.Model.ToEntity(product);
        request.Model.SeName = await _seNameService.ValidateSeName(product, request.Model.SeName, product.Name, true);
        product.SeName = request.Model.SeName;
        //search engine name
        await _slugService.SaveSlug(product, request.Model.SeName, "");
        //update product
        await _productService.UpdateProduct(product);

        //the stock of a product kept in one place changes through the stock journal, by the difference
        var stockChanged = product.StockQuantity != prevStockQuantity;
        var reservedChanged = product.ReservedQuantity != prevReservedQuantity;
        if ((stockChanged || reservedChanged) &&
            product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock && !product.UseMultipleWarehouses)
            await _stockMovementService.Apply(product.Id, [
                new StockChange([], "", stockChanged ? product.StockQuantity : 0, stockChanged,
                    InventoryMovementType.Api,
                    new StockSource("Api", product.Id, Reference: product.Sku ?? "",
                        CustomerId: _contextAccessor.WorkContext?.CurrentCustomer?.Id ?? "")) {
                    ReservedQuantity = reservedChanged ? product.ReservedQuantity : null
                }
            ]);

        switch (prevPublished)
        {
            //raise event 
            case false when product.Published:
                await _mediator.Publish(new ProductPublishEvent(product), cancellationToken);
                break;
            case true when !product.Published:
                await _mediator.Publish(new ProductUnPublishEvent(product), cancellationToken);
                break;
        }

        return product.ToModel();
    }
}
