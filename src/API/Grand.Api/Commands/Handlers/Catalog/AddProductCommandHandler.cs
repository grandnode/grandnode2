using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Domain.Seo;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
    {
        private readonly IProductService _productService;
        private readonly ISlugService _slugService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;
        private readonly SeoSettings _seoSettings;

        public AddProductCommandHandler(
            IProductService productService,
            ISlugService slugService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            ILanguageService languageService,
            SeoSettings seoSettings)
        {
            _productService = productService;
            _slugService = slugService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _languageService = languageService;
            _seoSettings = seoSettings;
        }

        public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken cancellationToken)
        {
            var product = request.Model.ToEntity();
            product.CreatedOnUtc = DateTime.UtcNow;
            product.UpdatedOnUtc = DateTime.UtcNow;
            await _productService.InsertProduct(product);

            request.Model.SeName = await product.ValidateSeName(request.Model.SeName, product.Name, true, _seoSettings, _slugService, _languageService);
            product.SeName = request.Model.SeName;
            //search engine name
            await _slugService.SaveSlug(product, request.Model.SeName, "");
            await _productService.UpdateProduct(product);

            //activity log
            await _customerActivityService.InsertActivity("AddNewProduct", product.Id, _translationService.GetResource("ActivityLog.AddNewProduct"), product.Name);

            return product.ToModel();
        }
    }
}
