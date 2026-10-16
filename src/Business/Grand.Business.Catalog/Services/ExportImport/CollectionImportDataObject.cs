using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.System;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Mapper;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

public class CollectionImportDataObject : IImportDataObject<CollectionDto>, IRowImport<CollectionDto>
{
    private readonly ICollectionLayoutService _collectionLayoutService;
    private readonly ICollectionService _collectionService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    private readonly SecurityConfig _securityConfig;
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly ILogger<CollectionImportDataObject> _logger;
    public CollectionImportDataObject(
        ICollectionService collectionService,
        IPictureService pictureService,
        ICollectionLayoutService collectionLayoutService,
        ISlugService slugService,
        ISeNameService seNameService,
        SecurityConfig securityConfig,
        ImportHtmlGuard htmlGuard,
        ILogger<CollectionImportDataObject> logger)
    {
        _collectionService = collectionService;
        _pictureService = pictureService;
        _collectionLayoutService = collectionLayoutService;
        _slugService = slugService;

        _seNameService = seNameService;
        _securityConfig = securityConfig;
        _htmlGuard = htmlGuard;
        _logger = logger;
    }

    public async Task Execute(IEnumerable<CollectionDto> data, CancellationToken cancellationToken = default)
    {
        var result = await Import(data.ToList(), false, ImportMode.Panel, cancellationToken);
        ImportRows.LogRejected(_logger, "collection", result);
        //what was saved stays saved; the panel must not report a cancelled import as done
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Row API: an unknown Id is rejected, a row without Id is matched by SeName, else (best effort) by Name</summary>
    public Task<ImportBatchResult> Import(IReadOnlyList<CollectionDto> rows, bool dryRun,
        CancellationToken cancellationToken = default)
    {
        return Import(rows, dryRun, ImportMode.Row, cancellationToken);
    }

    /// <summary>Row: unknown Id rejected, SeName/Name matching. Panel (XLSX): unknown Id creates the entity with that Id, no matching.</summary>
    private Task<ImportBatchResult> Import(IReadOnlyList<CollectionDto> rows, bool dryRun, ImportMode mode,
        CancellationToken cancellationToken = default)
    {
        return ImportRows.RunBatch(rows, dryRun, r => r.Id, Key,
            (row, dto) => ImportRow(row, dto, dryRun, mode, cancellationToken), mode == ImportMode.Row, _logger,
            cancellationToken);
    }

    private async Task<ImportRowResult> ImportRow(int row, CollectionDto dto, bool dryRun, ImportMode mode,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);

        var (existing, matchedByName, matchError) = await FindCollection(dto, mode);
        if (matchError != null)
            errors.Add(matchError);
        if (matchedByName && existing != null)
            warnings.Add(ImportRows.MatchedByNameWarning(existing.Name));
        if (!string.IsNullOrEmpty(dto.Id) && existing == null && mode == ImportMode.Row)
            errors.Add($"Id '{dto.Id}' was not found.");
        else if (existing == null && string.IsNullOrEmpty(dto.Name))
            errors.Add("Name is required for a new collection.");
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
        var collection = existing;
        var isNew = collection == null;
        if (collection == null) collection = dto.MapTo<CollectionDto, Collection>();
        else dto.MapTo(collection);

        if (!ValidCollection(collection))
        {
            errors.Add("Name is required for a new collection.");
            return new ImportRowResult(row, ImportRowStatus.Rejected, "", key, errors, warnings);
        }

        //downloaded before anything is written, so a cancelled download leaves the row untouched
        var image = await ImportPictures.Download(dto.Picture, _securityConfig.PictureImportAllowedPrivateHosts,
            cancellationToken);

        if (isNew) await _collectionService.InsertCollection(collection);
        else await _collectionService.UpdateCollection(collection);

        await UpdateCollectionData(collection, image);
        return new ImportRowResult(row, isNew ? ImportRowStatus.Created : ImportRowStatus.Updated, collection.Id, key,
            errors, warnings);
    }

    private static string Key(CollectionDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ? dto.Name : !string.IsNullOrEmpty(dto.SeName) ? dto.SeName : dto.Id ?? "";
    }

    /// <summary>
    ///     Resolves the existing entity by Id; in Row mode, when no Id is given, by SeName, and with neither, by its stored
    ///     name (any case) - more than one collection of that name is an error
    /// </summary>
    private async Task<(Collection collection, bool matchedByName, string error)> FindCollection(CollectionDto dto,
        ImportMode mode)
    {
        if (!string.IsNullOrEmpty(dto.Id))
            return (await _collectionService.GetCollectionById(dto.Id), false, null);

        if (mode == ImportMode.Panel)
            return (null, false, null);

        if (!string.IsNullOrEmpty(dto.SeName))
            return (await ImportRows.FindBySlug(_slugService, dto.SeName, EntityTypes.Collection,
                _collectionService.GetCollectionById), false, null);

        if (string.IsNullOrEmpty(dto.Name))
            return (null, false, null);

        var candidates = await _collectionService.GetAllCollections(dto.Name, "", customer: null, showHidden: true);
        var (found, error) = ImportRows.SingleByName(candidates, c => c.Name, dto.Name, "collection");
        return (found, found != null, error);
    }

    private async Task UpdateCollectionData(Collection collection, DownloadedImage image)
    {
        if (string.IsNullOrEmpty(collection.CollectionLayoutId))
        {
            collection.CollectionLayoutId =
                (await _collectionLayoutService.GetAllCollectionLayouts()).FirstOrDefault()?.Id;
        }
        else
        {
            var layout = await _collectionLayoutService.GetCollectionLayoutById(collection.CollectionLayoutId);
            if (layout == null)
                collection.CollectionLayoutId =
                    (await _collectionLayoutService.GetAllCollectionLayouts()).FirstOrDefault()?.Id;
        }

        var picture = await ImportPictures.Save(_pictureService, image, collection.Name, collection.PictureId);
        if (picture != null)
            collection.PictureId = picture.Id;

        var sename = collection.SeName ?? collection.Name;
        sename = await _seNameService.ValidateSeName(collection, sename, collection.Name, true);
        collection.SeName = sename;

        await _collectionService.UpdateCollection(collection);
        await _slugService.SaveSlug(collection, sename, "");
    }

    private static bool ValidCollection(Collection collection)
    {
        return !string.IsNullOrEmpty(collection.Name);
    }
}