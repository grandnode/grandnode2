﻿using Grand.Business.Catalog.Extensions;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Infrastructure.Mapper;

namespace Grand.Business.Catalog.Services.ExportImport;

public class CategoryImportDataObject : IImportDataObject<CategoryDto>
{
    private readonly ICategoryLayoutService _categoryLayoutService;
    private readonly ICategoryService _categoryService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    
    public CategoryImportDataObject(
        ICategoryService categoryService,
        IPictureService pictureService,
        ICategoryLayoutService categoryLayoutService,
        ISlugService slugService,
        ISeNameService seNameService)
    {
        _categoryService = categoryService;
        _pictureService = pictureService;
        _categoryLayoutService = categoryLayoutService;
        _slugService = slugService;
        _seNameService = seNameService;
    }

    public async Task Execute(IEnumerable<CategoryDto> data)
    {
        foreach (var item in data) await Import(item);
    }

    private async Task Import(CategoryDto categoryDto)
    {
        var category = await _categoryService.GetCategoryById(categoryDto.Id);
        var isNew = category == null;
        if (category == null) category = categoryDto.MapTo<CategoryDto, Category>();
        else categoryDto.MapTo(category);

        if (!ValidCategory(category)) return;

        if (isNew) await _categoryService.InsertCategory(category);
        else await _categoryService.UpdateCategory(category);

        await UpdateCategoryData(categoryDto, category);
    }

    private async Task UpdateCategoryData(CategoryDto categoryDto, Category category)
    {
        if (string.IsNullOrEmpty(category.CategoryLayoutId))
        {
            category.CategoryLayoutId = (await _categoryLayoutService.GetAllCategoryLayouts()).FirstOrDefault()?.Id;
        }
        else
        {
            var layout = await _categoryLayoutService.GetCategoryLayoutById(category.CategoryLayoutId);
            if (layout == null)
                category.CategoryLayoutId = (await _categoryLayoutService.GetAllCategoryLayouts()).FirstOrDefault()?.Id;
        }

        if (!string.IsNullOrEmpty(category.ParentCategoryId))
        {
            var parentCategory = await _categoryService.GetCategoryById(category.ParentCategoryId);
            if (parentCategory == null)
                category.ParentCategoryId = string.Empty;
        }

        if (!string.IsNullOrEmpty(categoryDto.Picture))
        {
            var picture = await LoadPicture(categoryDto.Picture, category.Name, category.PictureId);
            if (picture != null)
                category.PictureId = picture.Id;
        }

        var seName = category.SeName ?? category.Name;
        seName = await _seNameService.ValidateSeName(category, seName, category.Name, true);
        category.SeName = seName;
        await _categoryService.UpdateCategory(category);
        await _slugService.SaveSlug(category, seName, "");
    }

    private static bool ValidCategory(Category category)
    {
        return !string.IsNullOrEmpty(category.Name);
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