using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IContextAccessor _contextAccessor;
    private readonly IProductService _productService;
    private readonly IStockMovementService _stockMovementService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    public AddProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        ISeNameService seNameService,
        IStockMovementService stockMovementService,
        IContextAccessor contextAccessor)
    {
        _productService = productService;
        _stockMovementService = stockMovementService;
        _contextAccessor = contextAccessor;
        _slugService = slugService;
        _seNameService = seNameService;
    }

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = request.Model.ToEntity();
        //the stock the product starts with goes into the journal before the product exists
        await _stockMovementService.JournalNewProduct(product, InventoryMovementType.Api,
            new StockSource("Api", product.Id, Reference: product.Sku ?? "",
                CustomerId: _contextAccessor.WorkContext?.CurrentCustomer?.Id ?? ""));
        await _productService.InsertProduct(product);

        request.Model.SeName = await _seNameService.ValidateSeName(product, request.Model.SeName, product.Name, true);
        product.SeName = request.Model.SeName;
        //search engine name
        await _slugService.SaveSlug(product, request.Model.SeName, "");
        await _productService.UpdateProduct(product);

        return product.ToModel();
    }
}