using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallBrands()
    {
        var sampleImagesPath = GetSamplesPath();

        var brandLayoutInGridAndLines =
            _brandLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Grid or Lines");
        if (brandLayoutInGridAndLines == null)
            throw new Exception("Brand layout cannot be loaded");

        var allbrands = new List<Brand>();
        var brandXiaomi = new Brand {
            Name = "Xiaomi",
            BrandLayoutId = brandLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            Published = true,
            DisplayOrder = 1
        };
        brandXiaomi.PictureId = (await _pictureRepository.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "brand_xiaomi.jpg"), "image/pjpeg",
            "Xiaomi", "", "", false, Reference.Brand, brandXiaomi.Id)).Id;
        await _brandRepository.InsertAsync(brandXiaomi);
        allbrands.Add(brandXiaomi);


        var brandDell = new Brand {
            Name = "Dell",
            BrandLayoutId = brandLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            Published = true,
            DisplayOrder = 5
        };
        brandDell.PictureId = (await _pictureRepository.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "brand_dell.jpg"), "image/pjpeg",
            ("Dell"), "", "", false, Reference.Brand, brandDell.Id)).Id;

        await _brandRepository.InsertAsync(brandDell);
        allbrands.Add(brandDell);


        var brandAdidas = new Brand {
            Name = "Adidas",
            BrandLayoutId = brandLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            Published = true,
            DisplayOrder = 5
        };
        brandAdidas.PictureId = (await _pictureRepository.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "brand_adidas.jpg"), "image/pjpeg",
            ("Adidas"), "", "", false, Reference.Brand, brandAdidas.Id)).Id;
        await _brandRepository.InsertAsync(brandAdidas);
        allbrands.Add(brandAdidas);

        //search engine names
        foreach (var brand in allbrands)
        {
            brand.SeName = SeoExtensions.GenerateSlug(brand.Name, false, false, true);
            await _entityUrlRepository.InsertAsync(new EntityUrl {
                EntityId = brand.Id,
                EntityName = "Brand",
                LanguageId = "",
                IsActive = true,
                Slug = brand.SeName
            });
            await _brandRepository.UpdateAsync(brand);
        }
    }
}