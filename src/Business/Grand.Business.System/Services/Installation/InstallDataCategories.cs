using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
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
        var categoryComputers = new Category {
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
            DisplayOrder = 100
        };
        categoryComputers.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_computers.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Computers"), reference: Reference.Category,
            objectId: categoryComputers.Id)).Id;
        allCategories.Add(categoryComputers);

        var categoryTablets = new Category {
            Name = "Tablets",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categoryComputers.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categoryComputers.DisplayOrder + 1
        };
        categoryTablets.PictureId = (await pictureService.InsertPicture(
                File.ReadAllBytes(sampleImagesPath + "category_tablets.jpg"), "image/jpeg",
                pictureService.GetPictureSeName("Tablets"), reference: Reference.Category,
                objectId: categoryTablets.Id))
            .Id;
        allCategories.Add(categoryTablets);

        var categoryNotebooks = new Category {
            Name = "Notebooks",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categoryComputers.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categoryComputers.DisplayOrder + 2
        };
        categoryNotebooks.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_notebooks.jpg"), "image/jpeg",
            pictureService.GetPictureSeName("Notebooks"), reference: Reference.Category,
            objectId: categoryNotebooks.Id)).Id;
        allCategories.Add(categoryNotebooks);

        var categorySmartwatches = new Category {
            Name = "Smartwatches",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categoryComputers.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categoryComputers.DisplayOrder + 3
        };
        categorySmartwatches.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_smartwatches.jpg"), "image/jpeg",
            pictureService.GetPictureSeName("Smartwatches"), reference: Reference.Category,
            objectId: categorySmartwatches.Id)).Id;
        allCategories.Add(categorySmartwatches);

        var categoryElectronics = new Category {
            Name = "Electronics",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            ParentCategoryId = "",
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            IncludeInMenu = true,
            Published = true,
            ShowOnHomePage = false,
            DisplayOrder = 200
        };
        categoryElectronics.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_electronics.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Electronics"), reference: Reference.Category,
            objectId: categoryElectronics.Id)).Id;
        allCategories.Add(categoryElectronics);

        var categoryDisplay = new Category {
            Name = "Display",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categoryElectronics.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categoryElectronics.DisplayOrder + 1
        };
        categoryDisplay.PictureId = (await pictureService.InsertPicture(
                File.ReadAllBytes(sampleImagesPath + "category_display.jpeg"), "image/jpeg",
                pictureService.GetPictureSeName("Display"), reference: Reference.Category,
                objectId: categoryDisplay.Id))
            .Id;
        allCategories.Add(categoryDisplay);

        var categorySmartphones = new Category {
            Name = "Smartphones",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categoryElectronics.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categoryElectronics.DisplayOrder + 2
        };
        categorySmartphones.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_smartphones.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Smartphones"), reference: Reference.Category,
            objectId: categorySmartphones.Id)).Id;
        allCategories.Add(categorySmartphones);

        var categoryOthers = new Category {
            Name = "Others",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            ShowOnHomePage = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categoryElectronics.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categoryElectronics.DisplayOrder + 3
        };
        categoryOthers.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_accessories.jpg"), "image/jpeg",
            pictureService.GetPictureSeName("SmAccessoriesartphones"), reference: Reference.Category,
            objectId: categoryOthers.Id)).Id;

        allCategories.Add(categoryOthers);

        var categorySport = new Category {
            Name = "Sport",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 9,
            ParentCategoryId = "",
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9, 12",
            IncludeInMenu = true,
            Published = true,
            ShowOnHomePage = true,
            DisplayOrder = 300
        };
        categorySport.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_sport.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Sport"), reference: Reference.Category, objectId: categorySport.Id)).Id;
        allCategories.Add(categorySport);

        var categoryShoes = new Category {
            Name = "Shoes",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categorySport.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categorySport.DisplayOrder + 1
        };
        categoryShoes.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_shoes.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Shoes"), reference: Reference.Category, objectId: categoryShoes.Id)).Id;
        allCategories.Add(categoryShoes);

        var categoryApparel = new Category {
            Name = "Apparel",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            ParentCategoryId = categorySport.Id,
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categorySport.DisplayOrder + 2
        };
        categoryApparel.PictureId = (await pictureService.InsertPicture(
                File.ReadAllBytes(sampleImagesPath + "category_sport.jpeg"), "image/jpeg",
                pictureService.GetPictureSeName("Apparel"), reference: Reference.Category,
                objectId: categoryApparel.Id))
            .Id;

        allCategories.Add(categoryApparel);

        var categoryBalls = new Category {
            Name = "Balls",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            ParentCategoryId = categorySport.Id,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = categorySport.DisplayOrder + 3
        };
        categoryBalls.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_balls.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Balls"), reference: Reference.Category, objectId: categoryBalls.Id)).Id;
        allCategories.Add(categoryBalls);

        var categoryDigitalDownloads = new Category {
            Name = "Digital downloads",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            ParentCategoryId = "",
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            IncludeInMenu = true,
            Published = true,
            ShowOnHomePage = false,
            DisplayOrder = 400
        };
        categoryDigitalDownloads.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_digital_downloads.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName("Digital downloads"), reference: Reference.Category,
            objectId: categoryDigitalDownloads.Id)).Id;
        allCategories.Add(categoryDigitalDownloads);

        var categoryLego = new Category {
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
            DisplayOrder = 500,
            Flag = "Promo!",
            FlagStyle = "bg-success"
        };
        categoryLego.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_lego.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName(categoryLego.Name), reference: Reference.Category,
            objectId: categoryLego.Id)).Id;
        allCategories.Add(categoryLego);

        var categoryGiftVouchers = new Category {
            Name = "Gift vouchers",
            CategoryLayoutId = categoryLayoutInGridAndLines.Id,
            PageSize = 6,
            ParentCategoryId = "",
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = "6, 3, 9",
            IncludeInMenu = true,
            Published = true,
            DisplayOrder = 600
        };
        categoryGiftVouchers.PictureId = (await pictureService.InsertPicture(
            File.ReadAllBytes(sampleImagesPath + "category_gift_cards.jpeg"), "image/jpeg",
            pictureService.GetPictureSeName(categoryGiftVouchers.Name), reference: Reference.Category,
            objectId: categoryGiftVouchers.Id)).Id;
        allCategories.Add(categoryGiftVouchers);

        allCategories.ForEach(x => _categoryRepository.Insert(x));

        //search engine names
        foreach (var category in allCategories)
        {
            category.SeName = SeoExtensions.GenerateSlug(category.Name, false, false, false);
            await _entityUrlRepository.InsertAsync(new EntityUrl {
                EntityId = category.Id,
                EntityName = "Category",
                LanguageId = "",
                IsActive = true,
                Slug = category.SeName
            });
            await _categoryRepository.UpdateAsync(category);
        }
    }
}