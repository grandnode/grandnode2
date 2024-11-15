using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IMediator _mediator;
    private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
    private readonly IProductService _productService;
    private readonly ISlugService _slugService;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly ISeNameService _seNameService;
    public UpdateProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        IOutOfStockSubscriptionService outOfStockSubscriptionService,
        IStockQuantityService stockQuantityService,
        IMediator mediator,
        ISeNameService seNameService)
    {
        _productService = productService;
        _slugService = slugService;
        _outOfStockSubscriptionService = outOfStockSubscriptionService;
        _stockQuantityService = stockQuantityService;
        _mediator = mediator;
        _seNameService = seNameService;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        //product
        var product = await _productService.GetProductById(request.Model.Id);
        var prevStockQuantity = product.StockQuantity;
        var prevPublished = product.Published;

        product = request.Model.ToEntity(product);
        request.Model.SeName = await _seNameService.ValidateSeName(product, request.Model.SeName, product.Name, true);
        product.SeName = request.Model.SeName;
        //search engine name
        await _slugService.SaveSlug(product, request.Model.SeName, "");
        //update product
        await _productService.UpdateProduct(product);

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock &&
            product.BackorderModeId == BackorderMode.NoBackorders &&
            product.AllowOutOfStockSubscriptions &&
            _stockQuantityService.GetTotalStockQuantity(product) > 0 &&
            prevStockQuantity <= 0 && product.Published)
            await _outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");

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