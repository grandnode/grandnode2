using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Seo;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class AddBrandCommandHandler : IRequestHandler<AddBrandCommand, BrandDto>
{
    private readonly IBrandService _brandService;
    private readonly ILanguageService _languageService;
    private readonly SeoSettings _seoSettings;
    private readonly ISlugService _slugService;

    public AddBrandCommandHandler(
        IBrandService brandService,
        ISlugService slugService,
        ILanguageService languageService,
        SeoSettings seoSettings)
    {
        _brandService = brandService;
        _slugService = slugService;
        _languageService = languageService;
        _seoSettings = seoSettings;
    }

    public async Task<BrandDto> Handle(AddBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = request.Model.ToEntity();
        await _brandService.InsertBrand(brand);
        request.Model.SeName = await brand.ValidateSeName(request.Model.SeName, brand.Name, true, _seoSettings,
            _slugService, _languageService);
        brand.SeName = request.Model.SeName;
        await _brandService.UpdateBrand(brand);
        await _slugService.SaveSlug(brand, request.Model.SeName, "");

        return brand.ToModel();
    }
}