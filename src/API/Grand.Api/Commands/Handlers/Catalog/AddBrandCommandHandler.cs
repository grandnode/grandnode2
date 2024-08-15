using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class AddBrandCommandHandler : IRequestHandler<AddBrandCommand, BrandDto>
{
    private readonly IBrandService _brandService;
    private readonly ISlugService _slugService;
    private readonly ISlugNameValidator _slugNameValidator;
    public AddBrandCommandHandler(
        IBrandService brandService,
        ISlugService slugService,
        ISlugNameValidator slugNameValidator)
    {
        _brandService = brandService;
        _slugService = slugService;
        _slugNameValidator = slugNameValidator;
    }

    public async Task<BrandDto> Handle(AddBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = request.Model.ToEntity();
        await _brandService.InsertBrand(brand);
        request.Model.SeName = await _slugNameValidator.ValidateSeName(brand, request.Model.SeName, brand.Name, true);
        brand.SeName = request.Model.SeName;
        await _brandService.UpdateBrand(brand);
        await _slugService.SaveSlug(brand, request.Model.SeName, "");

        return brand.ToModel();
    }
}