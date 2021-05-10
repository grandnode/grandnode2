using Grand.Business.Common.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Seo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallBrands()
        {
            var pictureService = _serviceProvider.GetRequiredService<IPictureService>();
            var downloadService = _serviceProvider.GetRequiredService<IDownloadService>();

            var sampleImagesPath = GetSamplesPath();

            var brandLayoutInGridAndLines =
                _brandLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Grid or Lines");
            if (brandLayoutInGridAndLines == null)
                throw new Exception("Brand layout cannot be loaded");

            var allbrands = new List<Brand>();
            var brandXiaomi = new Brand
            {
                Name = "Xiaomi",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            brandXiaomi.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "brand_xiaomi.jpg"), "image/pjpeg", pictureService.GetPictureSeName("Xiaomi"), "", "", false, Domain.Common.Reference.Brand, brandXiaomi.Id)).Id;
            await _brandRepository.InsertAsync(brandXiaomi);
            allbrands.Add(brandXiaomi);


            var brandDell = new Brand
            {
                Name = "Dell",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                Published = true,
                DisplayOrder = 5,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            brandDell.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "brand_dell.jpg"), "image/pjpeg", pictureService.GetPictureSeName("Dell"), "", "", false, Domain.Common.Reference.Brand, brandDell.Id)).Id;

            await _brandRepository.InsertAsync(brandDell);
            allbrands.Add(brandDell);


            var brandAdidas = new Brand
            {
                Name = "Adidas",
                BrandLayoutId = brandLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                Published = true,
                DisplayOrder = 5,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            brandAdidas.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "brand_adidas.jpg"), "image/pjpeg", pictureService.GetPictureSeName("Adidas"), "", "", false, Domain.Common.Reference.Brand, brandAdidas.Id)).Id;
            await _brandRepository.InsertAsync(brandAdidas);
            allbrands.Add(brandAdidas);

            //search engine names
            foreach (var brand in allbrands)
            {
                brand.SeName = SeoExtensions.GenerateSlug(brand.Name, false, false, true);
                await _entityUrlRepository.InsertAsync(new EntityUrl
                {
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
}
