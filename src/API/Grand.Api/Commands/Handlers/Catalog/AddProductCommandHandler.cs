using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly ILanguageService _languageService;
    private readonly IProductService _productService;

    private readonly SeoSettings _seoSettings;
    private readonly ISlugService _slugService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public AddProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        ITranslationService translationService,
        ILanguageService languageService,
        IWorkContext workContext,
        SeoSettings seoSettings)
    {
        _productService = productService;
        _slugService = slugService;
        _translationService = translationService;
        _languageService = languageService;
        _workContext = workContext;
        _seoSettings = seoSettings;
    }

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = request.Model.ToEntity();
        await _productService.InsertProduct(product);

        request.Model.SeName = await product.ValidateSeName(request.Model.SeName, product.Name, true, _seoSettings,
            _slugService, _languageService);
        product.SeName = request.Model.SeName;
        //search engine name
        await _slugService.SaveSlug(product, request.Model.SeName, "");
        await _productService.UpdateProduct(product);

        return product.ToModel();
    }
}