using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly ILanguageService _languageService;
    private readonly IMediator _mediator;
    private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
    private readonly IProductService _productService;

    private readonly SeoSettings _seoSettings;
    private readonly ISlugService _slugService;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public UpdateProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        ITranslationService translationService,
        ILanguageService languageService,
        IOutOfStockSubscriptionService outOfStockSubscriptionService,
        IStockQuantityService stockQuantityService,
        IMediator mediator,
        IWorkContext workContext,
        SeoSettings seoSettings)
    {
        _productService = productService;
        _slugService = slugService;
        _translationService = translationService;
        _languageService = languageService;
        _outOfStockSubscriptionService = outOfStockSubscriptionService;
        _stockQuantityService = stockQuantityService;
        _mediator = mediator;
        _workContext = workContext;
        _seoSettings = seoSettings;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        //product
        var product = await _productService.GetProductById(request.Model.Id);
        var prevStockQuantity = product.StockQuantity;
        var prevPublished = product.Published;

        product = request.Model.ToEntity(product);
        request.Model.SeName = await product.ValidateSeName(request.Model.SeName, product.Name, true, _seoSettings,
            _slugService, _languageService);
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