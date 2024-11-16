using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    private readonly IBrandService _brandService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    public UpdateBrandCommandHandler(
        IBrandService brandService,
        ISlugService slugService,
        IPictureService pictureService,
        ISeNameService seNameService)
    {
        _brandService = brandService;
        _slugService = slugService;
        _pictureService = pictureService;
        _seNameService = seNameService;
    }

    public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandService.GetBrandById(request.Model.Id);
        var prevPictureId = brand.PictureId;
        brand = request.Model.ToEntity(brand);
        request.Model.SeName = await _seNameService.ValidateSeName(brand, request.Model.SeName, brand.Name, true);
        brand.SeName = request.Model.SeName;
        await _brandService.UpdateBrand(brand);
        //search engine name
        await _slugService.SaveSlug(brand, request.Model.SeName, "");
        await _brandService.UpdateBrand(brand);
        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != brand.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        if (!string.IsNullOrEmpty(brand.PictureId))
        {
            var picture = await _pictureService.GetPictureById(brand.PictureId);
            if (picture != null)
                await _pictureService.SetSeoFilename(picture, _pictureService.GetPictureSeName(brand.Name));
        }

        return brand.ToModel();
    }
}