using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Events.Models;
using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
    {
        private readonly IProductService _productService;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly ISlugService _slugService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
        private readonly IMediator _mediator;
        private readonly SeoSettings _seoSettings;

        public UpdateProductCommandHandler(
            IProductService productService,
            ISlugService slugService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            ILanguageService languageService,
            IOutOfStockSubscriptionService outOfStockSubscriptionService,
            IStockQuantityService stockQuantityService,
            IMediator mediator,
            SeoSettings seoSettings)
        {
            _productService = productService;
            _slugService = slugService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _languageService = languageService;
            _outOfStockSubscriptionService = outOfStockSubscriptionService;
            _stockQuantityService = stockQuantityService;
            _mediator = mediator;
            _seoSettings = seoSettings;
        }

        public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            //product
            var product = await _productService.GetProductById(request.Model.Id);
            var prevStockQuantity = product.StockQuantity;
            var prevPublished = product.Published;

            product = request.Model.ToEntity(product);
            product.UpdatedOnUtc = DateTime.UtcNow;
            request.Model.SeName = await product.ValidateSeName(request.Model.SeName, product.Name, true, _seoSettings, _slugService, _languageService);
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
            {
                await _outOfStockSubscriptionService.SendNotificationsToSubscribers(product, "");
            }

            //activity log
            await _customerActivityService.InsertActivity("EditProduct", product.Id, _translationService.GetResource("ActivityLog.EditProduct"), product.Name);

            //raise event 
            if (!prevPublished && product.Published)
                await _mediator.Publish(new ProductPublishEvent(product));

            if (prevPublished && !product.Published)
                await _mediator.Publish(new ProductUnPublishEvent(product));

            return product.ToModel();
        }
    }
}
