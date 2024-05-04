using Grand.Business.Catalog.Extensions;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;

namespace Grand.Business.Catalog.Services.ExportImport;

public class BrandImportDataObject : IImportDataObject<BrandDto>
{
    private readonly IBrandLayoutService _brandLayoutService;
    private readonly IBrandService _brandService;
    private readonly ILanguageService _languageService;
    private readonly IPictureService _pictureService;

    private readonly SeoSettings _seoSetting;
    private readonly ISlugService _slugService;

    public BrandImportDataObject(
        IBrandService brandService,
        IPictureService pictureService,
        IBrandLayoutService brandLayoutService,
        ISlugService slugService,
        ILanguageService languageService,
        SeoSettings seoSetting)
    {
        _brandService = brandService;
        _pictureService = pictureService;
        _brandLayoutService = brandLayoutService;
        _slugService = slugService;
        _languageService = languageService;

        _seoSetting = seoSetting;
    }

    public async Task Execute(IEnumerable<BrandDto> data)
    {
        foreach (var item in data) await Import(item);
    }

    private async Task Import(BrandDto brandDto)
    {
        var brand = await _brandService.GetBrandById(brandDto.Id);
        var isNew = brand == null;

        if (brand == null) brand = brandDto.MapTo<BrandDto, Brand>();
        else brandDto.MapTo(brand);

        if (!ValidBrand(brand)) return;

        if (isNew) await _brandService.InsertBrand(brand);
        else await _brandService.UpdateBrand(brand);

        await UpdateBrandData(brandDto, brand);
    }

    private async Task UpdateBrandData(BrandDto brandDto, Brand brand)
    {
        if (string.IsNullOrEmpty(brand.BrandLayoutId))
        {
            brand.BrandLayoutId = (await _brandLayoutService.GetAllBrandLayouts()).FirstOrDefault()?.Id;
        }
        else
        {
            var layout = await _brandLayoutService.GetBrandLayoutById(brand.BrandLayoutId);
            if (layout == null)
                brand.BrandLayoutId = (await _brandLayoutService.GetAllBrandLayouts()).FirstOrDefault()?.Id;
        }

        if (!string.IsNullOrEmpty(brandDto.Picture))
        {
            var picture = await LoadPicture(brandDto.Picture, brand.Name, brand.PictureId);
            if (picture != null)
                brand.PictureId = picture.Id;
        }

        var sename = brand.SeName ?? brand.Name;
        sename = await brand.ValidateSeName(sename, brand.Name, true, _seoSetting, _slugService, _languageService);
        brand.SeName = sename;

        await _brandService.UpdateBrand(brand);
        await _slugService.SaveSlug(brand, sename, "");
    }

    private bool ValidBrand(Brand brand)
    {
        return !string.IsNullOrEmpty(brand.Name);
    }

    /// <summary>
    ///     Creates or loads the image
    /// </summary>
    /// <param name="picturePath">The path to the image file</param>
    /// <param name="name">The name of the object</param>
    /// <param name="picId">Image identifier, may be null</param>
    /// <returns>The image or null if the image has not changed</returns>
    private async Task<Picture> LoadPicture(string picturePath, string name, string picId = "")
    {
        if (string.IsNullOrEmpty(picturePath) || !File.Exists(picturePath))
            return null;

        var mimeType = MimeTypeExtensions.GetMimeTypeFromFilePath(picturePath);
        var newPictureBinary = await File.ReadAllBytesAsync(picturePath);
        var pictureAlreadyExists = false;
        if (!string.IsNullOrEmpty(picId))
        {
            //compare with existing product pictures
            var existingPicture = await _pictureService.GetPictureById(picId);

            var existingBinary = await _pictureService.LoadPictureBinary(existingPicture);
            //picture binary after validation (like in database)
            var validatedPictureBinary = _pictureService.ValidatePicture(newPictureBinary, mimeType);
            if (existingBinary.SequenceEqual(validatedPictureBinary) ||
                existingBinary.SequenceEqual(newPictureBinary))
                pictureAlreadyExists = true;
        }

        if (pictureAlreadyExists) return null;

        var newPicture = await _pictureService.InsertPicture(newPictureBinary, mimeType,
            _pictureService.GetPictureSeName(name));
        return newPicture;
    }
}