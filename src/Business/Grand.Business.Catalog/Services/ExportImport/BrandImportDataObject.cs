using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Brands;
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

public class BrandImportDataObject : IImportDataObject<BrandDto>, IRowImport<BrandDto>
{
    private readonly IBrandLayoutService _brandLayoutService;
    private readonly IBrandService _brandService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    private readonly SecurityConfig _securityConfig;
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly SeoSettings _seoSettings;
    private readonly ILogger<BrandImportDataObject> _logger;
    
    public BrandImportDataObject(
        IBrandService brandService,
        IPictureService pictureService,
        IBrandLayoutService brandLayoutService,
        ISlugService slugService,
        ISeNameService seNameService,
        SecurityConfig securityConfig,
        ImportHtmlGuard htmlGuard,
        SeoSettings seoSettings,
        ILogger<BrandImportDataObject> logger)
    {
        _brandService = brandService;
        _pictureService = pictureService;
        _brandLayoutService = brandLayoutService;
        _slugService = slugService;
        _seNameService = seNameService;
        _securityConfig = securityConfig;
        _htmlGuard = htmlGuard;
        _seoSettings = seoSettings;
        _logger = logger;
    }

    public async Task Execute(IEnumerable<BrandDto> data)
    {
        var result = await Import(data.ToList(), false, ImportMode.Panel);
        ImportRows.LogRejected(_logger, "brand", result);
    }

    /// <summary>Row API: an unknown Id is rejected, a row without Id is matched by SeName, else (best effort) by Name</summary>
    public Task<ImportBatchResult> Import(IReadOnlyList<BrandDto> rows, bool dryRun,
        CancellationToken cancellationToken = default)
    {
        return Import(rows, dryRun, ImportMode.Row, cancellationToken);
    }

    /// <summary>
    ///     Row: unknown Id rejected, SeName matching. Panel (XLSX): unknown Id creates the entity with that Id, no SeName matching.
    /// </summary>
    private enum ImportMode
    {
        Row,
        Panel
    }

    private async Task<ImportBatchResult> Import(IReadOnlyList<BrandDto> rows, bool dryRun, ImportMode mode,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ImportRowResult>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            //a batch that runs out of time stops at a row boundary: what was saved is reported, the rest is not touched
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(ImportRowResult.NotProcessed(i + 1, rows[i].Id, Key(rows[i])));
                continue;
            }

            results.Add(await ImportRow(i + 1, rows[i], dryRun, mode));
        }

        return new ImportBatchResult(dryRun, results);
    }

    private async Task<ImportRowResult> ImportRow(int row, BrandDto dto, bool dryRun, ImportMode mode)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);

        var (existing, matchedByName) = await FindBrand(dto, mode);
        if (matchedByName)
            warnings.Add(ImportRows.MatchedByNameWarning(existing.Name));
        if (!string.IsNullOrEmpty(dto.Id) && existing == null && mode == ImportMode.Row)
            errors.Add($"Id '{dto.Id}' was not found.");
        else if (existing == null && string.IsNullOrEmpty(dto.Name))
            errors.Add("Name is required for a new brand.");
        if (dto.Name == "")
            errors.Add("Name cannot be empty.");

        errors.AddRange(_htmlGuard.RichTextErrors((nameof(dto.Description), dto.Description),
            (nameof(dto.BottomDescription), dto.BottomDescription)));
        errors.AddRange(_htmlGuard.PlainTextErrors((nameof(dto.Name), dto.Name), (nameof(dto.SeName), dto.SeName),
            (nameof(dto.MetaKeywords), dto.MetaKeywords), (nameof(dto.MetaDescription), dto.MetaDescription),
            (nameof(dto.MetaTitle), dto.MetaTitle)));

        if (errors.Count > 0)
            return new ImportRowResult(row, ImportRowStatus.Rejected, existing?.Id ?? "", key, errors, warnings);

        if (dryRun)
            return new ImportRowResult(row, existing == null ? ImportRowStatus.Created : ImportRowStatus.Updated,
                existing?.Id ?? dto.Id ?? "", key, errors, warnings);

        //an entity found by SeName keeps its own id
        if (existing != null) dto.Id = existing.Id;
        var brand = existing;
        var isNew = brand == null;
        if (brand == null) brand = dto.MapTo<BrandDto, Brand>();
        else dto.MapTo(brand);

        if (!ValidBrand(brand))
        {
            errors.Add("Name is required for a new brand.");
            return new ImportRowResult(row, ImportRowStatus.Rejected, "", key, errors, warnings);
        }

        if (isNew) await _brandService.InsertBrand(brand);
        else await _brandService.UpdateBrand(brand);

        await UpdateBrandData(dto, brand);
        return new ImportRowResult(row, isNew ? ImportRowStatus.Created : ImportRowStatus.Updated, brand.Id, key,
            errors, warnings);
    }

    private static string Key(BrandDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ? dto.Name : !string.IsNullOrEmpty(dto.SeName) ? dto.SeName : dto.Id ?? "";
    }

    /// <summary>
    ///     Resolves the existing entity by Id; in Row mode, when no Id is given, by SeName, and with neither, by the slug
    ///     its Name would get - accepted only when the stored name is the same (any case)
    /// </summary>
    private async Task<(Brand brand, bool matchedByName)> FindBrand(BrandDto dto, ImportMode mode)
    {
        if (!string.IsNullOrEmpty(dto.Id))
            return (await _brandService.GetBrandById(dto.Id), false);

        if (mode == ImportMode.Panel)
            return (null, false);

        if (!string.IsNullOrEmpty(dto.SeName))
            return (await FindBrandBySlug(dto.SeName), false);

        if (string.IsNullOrEmpty(dto.Name))
            return (null, false);

        var found = await FindBrandBySlug(ImportRows.NameSlug(dto.Name, _seoSettings));
        var matches = found != null && string.Equals(found.Name, dto.Name, StringComparison.OrdinalIgnoreCase);
        return matches ? (found, true) : (null, false);
    }

    private async Task<Brand> FindBrandBySlug(string slug)
    {
        var url = await _slugService.GetBySlug(slug);
        if (url is not { EntityName: EntityTypes.Brand } || string.IsNullOrEmpty(url.EntityId))
            return null;

        return await _brandService.GetBrandById(url.EntityId);
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

        var seName = brand.SeName ?? brand.Name;
        seName = await _seNameService.ValidateSeName(brand, seName, brand.Name, true);
        brand.SeName = seName;

        await _brandService.UpdateBrand(brand);
        await _slugService.SaveSlug(brand, seName, "");
    }

    private static bool ValidBrand(Brand brand)
    {
        return !string.IsNullOrEmpty(brand.Name);
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