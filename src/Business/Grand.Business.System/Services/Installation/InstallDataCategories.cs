using Grand.Business.Common.Extensions;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
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
        protected virtual async Task InstallCategories()
        {
            var pictureService = _serviceProvider.GetRequiredService<IPictureService>();

            //sample pictures
            var sampleImagesPath = GetSamplesPath();

            var categoryLayoutInGridAndLines = _categoryLayoutRepository
                .Table.FirstOrDefault(pt => pt.Name == "Grid or Lines");
            if (categoryLayoutInGridAndLines == null)
                throw new Exception("Category layout cannot be loaded");


            //categories
            var allCategories = new List<Category>();
            var categoryComputers = new Category
            {
                Name = "Computers",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = "",
                IncludeInMenu = true,
                Published = true,
                Flag = "New",
                FlagStyle = "badge-danger",
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryComputers.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_computers.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Computers"), reference: Domain.Common.Reference.Category, objectId: categoryComputers.Id)).Id;
            allCategories.Add(categoryComputers);

            var categoryTablets = new Category
            {
                Name = "Tablets",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categoryComputers.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryTablets.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_tablets.jpg"), "image/jpeg", pictureService.GetPictureSeName("Tablets"), reference: Domain.Common.Reference.Category, objectId: categoryTablets.Id)).Id;
            allCategories.Add(categoryTablets);

            var categoryNotebooks = new Category
            {
                Name = "Notebooks",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categoryComputers.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 2,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryNotebooks.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_notebooks.jpg"), "image/jpeg", pictureService.GetPictureSeName("Notebooks"), reference: Domain.Common.Reference.Category, objectId: categoryNotebooks.Id)).Id;
            allCategories.Add(categoryNotebooks);

            var categorySmartwatches = new Category
            {
                Name = "Smartwatches",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categoryComputers.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 3,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categorySmartwatches.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_smartwatches.jpg"), "image/jpeg", pictureService.GetPictureSeName("Smartwatches"), reference: Domain.Common.Reference.Category, objectId: categorySmartwatches.Id)).Id;
            allCategories.Add(categorySmartwatches);

            var categoryElectronics = new Category
            {
                Name = "Electronics",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                ParentCategoryId = "",
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                IncludeInMenu = true,
                Published = true,
                ShowOnHomePage = false,
                DisplayOrder = 2,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryElectronics.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_electronics.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Electronics"), reference: Domain.Common.Reference.Category, objectId: categoryElectronics.Id)).Id;
            allCategories.Add(categoryElectronics);

            var categoryDisplay = new Category
            {
                Name = "Display",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categoryElectronics.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryDisplay.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_display.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Display"), reference: Domain.Common.Reference.Category, objectId: categoryDisplay.Id)).Id;
            allCategories.Add(categoryDisplay);

            var categorySmartphones = new Category
            {
                Name = "Smartphones",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categoryElectronics.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 2,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categorySmartphones.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_smartphones.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Smartphones"), reference: Domain.Common.Reference.Category, objectId: categorySmartphones.Id)).Id;
            allCategories.Add(categorySmartphones);

            var categoryOthers = new Category
            {
                Name = "Others",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                ShowOnHomePage = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categoryElectronics.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 3,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryOthers.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_accessories.jpg"), "image/jpeg", pictureService.GetPictureSeName("SmAccessoriesartphones"), reference: Domain.Common.Reference.Category, objectId: categoryOthers.Id)).Id;

            allCategories.Add(categoryOthers);

            var categorySport = new Category
            {
                Name = "Sport",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 9,
                ParentCategoryId = "",
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9, 12",
                IncludeInMenu = true,
                Published = true,
                ShowOnHomePage = true,
                DisplayOrder = 3,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categorySport.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_sport.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Sport"), reference: Domain.Common.Reference.Category, objectId: categorySport.Id)).Id;
            allCategories.Add(categorySport);

            var categoryShoes = new Category
            {
                Name = "Shoes",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categorySport.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 1,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryShoes.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_shoes.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Shoes"), reference: Domain.Common.Reference.Category, objectId: categoryShoes.Id)).Id;
            allCategories.Add(categoryShoes);

            var categoryApparel = new Category
            {
                Name = "Apparel",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                ParentCategoryId = categorySport.Id,
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 2,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryApparel.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_sport.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Apparel"), reference: Domain.Common.Reference.Category, objectId: categoryApparel.Id)).Id;

            allCategories.Add(categoryApparel);

            var categoryBalls = new Category
            {
                Name = "Balls",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                ParentCategoryId = categorySport.Id,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 6,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryBalls.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_balls.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Balls"), reference: Domain.Common.Reference.Category, objectId: categoryBalls.Id)).Id;
            allCategories.Add(categoryBalls);

            var categoryDigitalDownloads = new Category
            {
                Name = "Digital downloads",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                ParentCategoryId = "",
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                IncludeInMenu = true,
                Published = true,
                ShowOnHomePage = false,
                DisplayOrder = 4,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryDigitalDownloads.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_digital_downloads.jpeg"), "image/jpeg", pictureService.GetPictureSeName("Digital downloads"), reference: Domain.Common.Reference.Category, objectId: categoryDigitalDownloads.Id)).Id;
            allCategories.Add(categoryDigitalDownloads);

            var categoryLego = new Category
            {
                Name = "Lego",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                MetaKeywords = "Lego, Dictionary, Textbooks",
                MetaDescription = "Books category description",
                PageSize = 6,
                ParentCategoryId = "",
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                IncludeInMenu = true,
                Published = true,
                ShowOnHomePage = true,
                DisplayOrder = 5,
                Flag = "Promo!",
                FlagStyle = "bg-success",
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryLego.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_lego.jpeg"), "image/jpeg", pictureService.GetPictureSeName(categoryLego.Name), reference: Domain.Common.Reference.Category, objectId: categoryLego.Id)).Id;
            allCategories.Add(categoryLego);

            var categoryGiftVouchers = new Category
            {
                Name = "Gift vouchers",
                CategoryLayoutId = categoryLayoutInGridAndLines.Id,
                PageSize = 6,
                ParentCategoryId = "",
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9",
                IncludeInMenu = true,
                Published = true,
                DisplayOrder = 7,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            categoryGiftVouchers.PictureId = (await pictureService.InsertPicture(File.ReadAllBytes(sampleImagesPath + "category_gift_cards.jpeg"), "image/jpeg", pictureService.GetPictureSeName(categoryGiftVouchers.Name), reference: Domain.Common.Reference.Category, objectId: categoryGiftVouchers.Id)).Id;
            allCategories.Add(categoryGiftVouchers);

            await _categoryRepository.InsertAsync(allCategories);
            //search engine names
            foreach (var category in allCategories)
            {
                category.SeName = SeoExtensions.GenerateSlug(category.Name, false, false, false);
                await _entityUrlRepository.InsertAsync(new EntityUrl
                {
                    EntityId = category.Id,
                    EntityName = "Category",
                    LanguageId = "",
                    IsActive = true,
                    Slug = category.SeName,
                });
                await _categoryRepository.UpdateAsync(category);
            }
        }
    }
}
