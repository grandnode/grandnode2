using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>Row API only: there is no XLSX import for product attributes</summary>
public class ProductAttributeImportDataObject : IRowImport<ProductAttributeDto>
{
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly IProductAttributeService _productAttributeService;
    private readonly SeoSettings _seoSettings;

    public ProductAttributeImportDataObject(IProductAttributeService productAttributeService,
        SeoSettings seoSettings, ImportHtmlGuard htmlGuard)
    {
        _productAttributeService = productAttributeService;
        _seoSettings = seoSettings;
        _htmlGuard = htmlGuard;
    }

    public async Task<ImportBatchResult> Import(IReadOnlyList<ProductAttributeDto> rows, bool dryRun,
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

            results.Add(await ImportRow(i + 1, rows[i], dryRun));
        }

        return new ImportBatchResult(dryRun, results);
    }

    private async Task<ImportRowResult> ImportRow(int row, ProductAttributeDto dto, bool dryRun)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var key = Key(dto);

        var (existing, matchedByName) = await Find(dto);
        if (matchedByName)
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

        if (errors.Count > 0)
            return new ImportRowResult(row, ImportRowStatus.Rejected, existing?.Id ?? "", key, errors, warnings);

        if (dryRun)
            return new ImportRowResult(row, existing == null ? ImportRowStatus.Created : ImportRowStatus.Updated,
                existing?.Id ?? "", key, errors, warnings);

        var isNew = existing == null;
        var attribute = existing ?? new ProductAttribute { Name = dto.Name };
        if (!string.IsNullOrEmpty(dto.Name)) attribute.Name = dto.Name;
        if (dto.Description != null) attribute.Description = dto.Description;
        if (isNew || !string.IsNullOrEmpty(dto.SeName))
            attribute.SeName = GetSeName(string.IsNullOrEmpty(dto.SeName) ? attribute.Name : dto.SeName);

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

        if (isNew) await _productAttributeService.InsertProductAttribute(attribute);
        else await _productAttributeService.UpdateProductAttribute(attribute);

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
    private async Task<(ProductAttribute attribute, bool matchedByName)> Find(ProductAttributeDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Id))
            return (await _productAttributeService.GetProductAttributeById(dto.Id), false);

        if (!string.IsNullOrEmpty(dto.SeName))
            return (await FindBySeName(GetSeName(dto.SeName)), false);

        if (string.IsNullOrEmpty(dto.Name))
            return (null, false);

        var found = await FindBySeName(GetSeName(dto.Name));
        return found != null && string.Equals(found.Name, dto.Name, StringComparison.OrdinalIgnoreCase)
            ? (found, true)
            : (null, false);
    }

    //no lookup by SeName exists: load all and match
    private async Task<ProductAttribute> FindBySeName(string seName)
    {
        if (string.IsNullOrEmpty(seName))
            return null;

        var all = await _productAttributeService.GetAllProductAttributes();
        return all.FirstOrDefault(a => string.Equals(a.SeName, seName, StringComparison.OrdinalIgnoreCase));
    }

    //same normalization as the panel's product attribute controller
    private string GetSeName(string name)
    {
        return SeoExtensions.GetSeName(name, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls,
            _seoSettings.SeoCharConversion);
    }
}
