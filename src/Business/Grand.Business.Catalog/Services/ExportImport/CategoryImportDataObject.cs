using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.System;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Mapper;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

public class CategoryImportDataObject : IImportDataObject<CategoryDto>, IRowImport<CategoryDto>
{
    private readonly ICategoryLayoutService _categoryLayoutService;
    private readonly ICategoryService _categoryService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    private readonly SecurityConfig _securityConfig;
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly ILogger<CategoryImportDataObject> _logger;
    
    public CategoryImportDataObject(
        ICategoryService categoryService,
        IPictureService pictureService,
        ICategoryLayoutService categoryLayoutService,
        ISlugService slugService,
        ISeNameService seNameService,
        SecurityConfig securityConfig,
        ImportHtmlGuard htmlGuard,
        ILogger<CategoryImportDataObject> logger)
    {
        _categoryService = categoryService;
        _pictureService = pictureService;
        _categoryLayoutService = categoryLayoutService;
        _slugService = slugService;
        _seNameService = seNameService;
        _securityConfig = securityConfig;
        _htmlGuard = htmlGuard;
        _logger = logger;
    }

    public async Task Execute(IEnumerable<CategoryDto> data)
    {
        var result = await Import(data.ToList(), false, ImportMode.Panel);
        ImportRows.LogRejected(_logger, "category", result);
    }

    /// <summary>Row API: an unknown Id is rejected, a row without Id is matched by SeName, else (best effort) by Name</summary>
    public Task<ImportBatchResult> Import(IReadOnlyList<CategoryDto> rows, bool dryRun,
        CancellationToken cancellationToken = default)
    {
        return Import(rows, dryRun, ImportMode.Row, cancellationToken);
    }

    /// <summary>Row: unknown Id rejected, SeName/Name matching. Panel (XLSX): unknown Id creates the entity with that Id, no matching.</summary>
    private Task<ImportBatchResult> Import(IReadOnlyList<CategoryDto> rows, bool dryRun, ImportMode mode,
        CancellationToken cancellationToken = default)
    {
        return ImportRows.RunBatch(rows, dryRun, r => r.Id, Key, (row, dto) => ImportRow(row, dto, dryRun, mode),
            mode == ImportMode.Row, _logger, cancellationToken);
    }

    private async Task<ImportRowResult> ImportRow(int row, CategoryDto dto, bool dryRun, ImportMode mode)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);

        var (existing, matchedByName, matchError) = await FindCategory(dto, mode);
        if (matchError != null)
            errors.Add(matchError);
        if (matchedByName && existing != null)
            warnings.Add(ImportRows.MatchedByNameWarning(existing.Name));
        if (!string.IsNullOrEmpty(dto.Id) && existing == null && mode == ImportMode.Row)
            errors.Add($"Id '{dto.Id}' was not found.");
        else if (existing == null && string.IsNullOrEmpty(dto.Name))
            errors.Add("Name is required for a new category.");
        if (dto.Name == "")
            errors.Add("Name cannot be empty.");

        errors.AddRange(_htmlGuard.RichTextErrors((nameof(dto.Description), dto.Description),
            (nameof(dto.BottomDescription), dto.BottomDescription)));
        errors.AddRange(_htmlGuard.PlainTextErrors((nameof(dto.Name), dto.Name), (nameof(dto.SeName), dto.SeName),
            (nameof(dto.MetaKeywords), dto.MetaKeywords), (nameof(dto.MetaDescription), dto.MetaDescription),
            (nameof(dto.MetaTitle), dto.MetaTitle)));

        var parentMissing = !string.IsNullOrEmpty(dto.ParentCategoryId) &&
                            await _categoryService.GetCategoryById(dto.ParentCategoryId) == null;
        if (parentMissing)
            warnings.Add($"ParentCategoryId '{dto.ParentCategoryId}' was not found and was cleared.");

        if (errors.Count > 0)
            return new ImportRowResult(row, ImportRowStatus.Rejected, existing?.Id ?? "", key, errors, warnings);

        if (dryRun)
            return new ImportRowResult(row, existing == null ? ImportRowStatus.Created : ImportRowStatus.Updated,
                existing?.Id ?? dto.Id ?? "", key, errors, warnings);

        //an entity found by SeName keeps its own id
        if (existing != null) dto.Id = existing.Id;
        var category = existing;
        var isNew = category == null;
        if (category == null) category = dto.MapTo<CategoryDto, Category>();
        else dto.MapTo(category);

        if (!ValidCategory(category))
        {
            errors.Add("Name is required for a new category.");
            return new ImportRowResult(row, ImportRowStatus.Rejected, "", key, errors, warnings);
        }

        if (isNew) await _categoryService.InsertCategory(category);
        else await _categoryService.UpdateCategory(category);

        await UpdateCategoryData(dto, category, parentMissing);
        return new ImportRowResult(row, isNew ? ImportRowStatus.Created : ImportRowStatus.Updated, category.Id, key,
            errors, warnings);
    }

    private static string Key(CategoryDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ? dto.Name : !string.IsNullOrEmpty(dto.SeName) ? dto.SeName : dto.Id ?? "";
    }

    /// <summary>
    ///     Resolves the existing entity by Id; in Row mode, when no Id is given, by SeName, and with neither, by its stored
    ///     name (any case) - under the row's parent when it names one; more than one category of that name is an error
    /// </summary>
    private async Task<(Category category, bool matchedByName, string error)> FindCategory(CategoryDto dto,
        ImportMode mode)
    {
        if (!string.IsNullOrEmpty(dto.Id))
            return (await _categoryService.GetCategoryById(dto.Id), false, null);

        if (mode == ImportMode.Panel)
            return (null, false, null);

        if (!string.IsNullOrEmpty(dto.SeName))
            return (await ImportRows.FindBySlug(_slugService, dto.SeName, EntityTypes.Category,
                _categoryService.GetCategoryById), false, null);

        if (string.IsNullOrEmpty(dto.Name))
            return (null, false, null);

        var parentId = string.IsNullOrEmpty(dto.ParentCategoryId) ? null : dto.ParentCategoryId;
        var candidates = await _categoryService.GetAllCategories(parentId, dto.Name, "", showHidden: true);
        var (found, error) = ImportRows.SingleByName(candidates, c => c.Name, dto.Name, "category");
        return (found, found != null, error);
    }

    private async Task UpdateCategoryData(CategoryDto categoryDto, Category category, bool parentMissing)
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

        //the row's own parent was looked up already; only a stored one it did not replace is read here
        if (parentMissing)
            category.ParentCategoryId = string.Empty;
        else if (!string.IsNullOrEmpty(category.ParentCategoryId) &&
                 category.ParentCategoryId != categoryDto.ParentCategoryId &&
                 await _categoryService.GetCategoryById(category.ParentCategoryId) == null)
            category.ParentCategoryId = string.Empty;

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
    /// <param name="pictureUrl">The URL of the image</param>
    /// <param name="name">The name of the object</param>
    /// <param name="picId">Image identifier, may be null</param>
    /// <returns>The image or null if the image has not changed</returns>
    private async Task<Picture> LoadPicture(string pictureUrl, string name, string picId = "")
    {
        if (string.IsNullOrEmpty(pictureUrl))
            return null;

        //only http(s) URLs to public or explicitly allowed hosts are downloaded - a local path would let the
        //import read any file on the server
        var image = await DownloadUrl.DownloadImage(pictureUrl, _securityConfig.PictureImportAllowedPrivateHosts);
        if (image == null)
            return null;

        var mimeType = image.MimeType;
        var newPictureBinary = image.Binary;
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