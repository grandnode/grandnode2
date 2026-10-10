using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>Row API only: there is no XLSX import for product attributes</summary>
public class ProductAttributeImportDataObject : IRowImport<ProductAttributeDto>
{
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly ILogger<ProductAttributeImportDataObject> _logger;
    private readonly IProductAttributeService _productAttributeService;
    private readonly SeoSettings _seoSettings;

    public ProductAttributeImportDataObject(IProductAttributeService productAttributeService,
        SeoSettings seoSettings, ImportHtmlGuard htmlGuard, ILogger<ProductAttributeImportDataObject> logger)
    {
        _logger = logger;
        _productAttributeService = productAttributeService;
        _seoSettings = seoSettings;
        _htmlGuard = htmlGuard;
    }

    public async Task<ImportBatchResult> Import(IReadOnlyList<ProductAttributeDto> rows, bool dryRun,
        CancellationToken cancellationToken = default)
    {
        //there is no lookup by SeName: the attributes are loaded once per batch, and the ones it creates are added
        var all = (await _productAttributeService.GetAllProductAttributes()).ToList();
        return await ImportRows.RunBatch(rows, dryRun, r => r.Id, Key, (row, dto) => ImportRow(row, dto, dryRun, all),
            true, _logger, cancellationToken);
    }

    private async Task<ImportRowResult> ImportRow(int row, ProductAttributeDto dto, bool dryRun,
        List<ProductAttribute> all)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);

        var (existing, matchedByName) = await Find(dto, all);
        if (matchedByName && existing != null)
            warnings.Add(ImportRows.MatchedByNameWarning(existing.Name));
        if (!string.IsNullOrEmpty(dto.Id) && existing == null)
            errors.Add($"Id '{dto.Id}' was not found.");
        else if (existing == null && string.IsNullOrEmpty(dto.Name))
            errors.Add("Name is required for a new product attribute.");
        if (dto.Name == "")
            errors.Add("Name cannot be empty.");

        errors.AddRange(_htmlGuard.PlainTextErrors((nameof(dto.Name), dto.Name), (nameof(dto.SeName), dto.SeName)));
        errors.AddRange(_htmlGuard.RichTextErrors((nameof(dto.Description), dto.Description)));
        foreach (var value in dto.Values ?? [])
        {
            if (string.IsNullOrEmpty(value?.Name))
            {
                errors.Add("Value Name is required.");
                continue;
            }

            errors.AddRange(_htmlGuard.PlainTextErrors(("Value Name", value.Name)));
        }

        var seName = existing == null || !string.IsNullOrEmpty(dto.SeName)
            ? GetSeName(string.IsNullOrEmpty(dto.SeName) ? dto.Name : dto.SeName)
            : null;
        if (!string.IsNullOrEmpty(seName) && FindBySeName(all, seName) is { } owner && owner.Id != existing?.Id)
            errors.Add($"SeName '{seName}' is already used by '{owner.Name}'.");

        if (errors.Count > 0)
            return new ImportRowResult(row, ImportRowStatus.Rejected, existing?.Id ?? "", key, errors, warnings);

        if (dryRun)
            return new ImportRowResult(row, existing == null ? ImportRowStatus.Created : ImportRowStatus.Updated,
                existing?.Id ?? "", key, errors, warnings);

        var isNew = existing == null;
        var attribute = existing ?? new ProductAttribute { Name = dto.Name };
        if (!string.IsNullOrEmpty(dto.Name)) attribute.Name = dto.Name;
        if (dto.Description != null) attribute.Description = dto.Description;
        if (seName != null) attribute.SeName = seName;

        //a repeated name in the same row matches the value just added, so it collapses into it
        foreach (var valueDto in dto.Values ?? [])
        {
            var value = attribute.PredefinedProductAttributeValues
                .FirstOrDefault(v => string.Equals(v.Name, valueDto.Name, StringComparison.OrdinalIgnoreCase));
            if (value == null)
            {
                value = new PredefinedProductAttributeValue { Name = valueDto.Name };
                attribute.PredefinedProductAttributeValues.Add(value);
            }

            if (valueDto.PriceAdjustment.HasValue) value.PriceAdjustment = valueDto.PriceAdjustment.Value;
            if (valueDto.WeightAdjustment.HasValue) value.WeightAdjustment = valueDto.WeightAdjustment.Value;
            if (valueDto.Cost.HasValue) value.Cost = valueDto.Cost.Value;
            if (valueDto.IsPreSelected.HasValue) value.IsPreSelected = valueDto.IsPreSelected.Value;
            if (valueDto.DisplayOrder.HasValue) value.DisplayOrder = valueDto.DisplayOrder.Value;
        }

        if (isNew)
        {
            await _productAttributeService.InsertProductAttribute(attribute);
            all.Add(attribute);
        }
        else
        {
            await _productAttributeService.UpdateProductAttribute(attribute);
        }

        return new ImportRowResult(row, isNew ? ImportRowStatus.Created : ImportRowStatus.Updated, attribute.Id, key,
            errors, warnings);
    }

    private static string Key(ProductAttributeDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ? dto.Name : !string.IsNullOrEmpty(dto.SeName) ? dto.SeName : dto.Id ?? "";
    }

    /// <summary>
    ///     By Id, else by SeName normalized as a new attribute's is, else (best effort) by the SeName its Name would get,
    ///     accepted only when the stored name is the same (any case)
    /// </summary>
    private async Task<(ProductAttribute attribute, bool matchedByName)> Find(ProductAttributeDto dto,
        List<ProductAttribute> all)
    {
        if (!string.IsNullOrEmpty(dto.Id))
            return (await _productAttributeService.GetProductAttributeById(dto.Id), false);

        if (!string.IsNullOrEmpty(dto.SeName))
            return (FindBySeName(all, GetSeName(dto.SeName)), false);

        if (string.IsNullOrEmpty(dto.Name))
            return (null, false);

        var found = FindBySeName(all, GetSeName(dto.Name));
        return found != null && string.Equals(found.Name, dto.Name, StringComparison.OrdinalIgnoreCase)
            ? (found, true)
            : (null, false);
    }

    private static ProductAttribute FindBySeName(List<ProductAttribute> all, string seName)
    {
        return string.IsNullOrEmpty(seName)
            ? null
            : all.FirstOrDefault(a => string.Equals(a.SeName, seName, StringComparison.OrdinalIgnoreCase));
    }

    //same normalization as the panel's product attribute controller
    private string GetSeName(string name)
    {
        return SeoExtensions.GetSeName(name, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls,
            _seoSettings.SeoCharConversion);
    }
}
