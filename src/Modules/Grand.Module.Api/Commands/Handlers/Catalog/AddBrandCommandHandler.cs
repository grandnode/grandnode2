using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddBrandCommandHandler : IRequestHandler<AddBrandCommand, BrandDto>
{
    private readonly IBrandService _brandService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    public AddBrandCommandHandler(
        IBrandService brandService,
        ISlugService slugService,
        ISeNameService seNameService)
    {
        _brandService = brandService;
        _slugService = slugService;
        _seNameService = seNameService;
    }

    public async Task<BrandDto> Handle(AddBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = request.Model.ToEntity();
        await _brandService.InsertBrand(brand);
        request.Model.SeName = await _seNameService.ValidateSeName(brand, request.Model.SeName, brand.Name, true);
        brand.SeName = request.Model.SeName;
        await _brandService.UpdateBrand(brand);
        await _slugService.SaveSlug(brand, request.Model.SeName, "");

        return brand.ToModel();
    }
}