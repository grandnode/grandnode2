using Grand.Business.Core.Dto;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>Row API only: there is no XLSX import for specification attributes</summary>
public class SpecificationAttributeImportDataObject : IRowImport<SpecificationAttributeDto>
{
    private readonly ImportHtmlGuard _htmlGuard;
    private readonly SeoSettings _seoSettings;
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public SpecificationAttributeImportDataObject(ISpecificationAttributeService specificationAttributeService,
        SeoSettings seoSettings, ImportHtmlGuard htmlGuard)
    {
        _specificationAttributeService = specificationAttributeService;
        _seoSettings = seoSettings;
        _htmlGuard = htmlGuard;
    }

    public async Task<ImportBatchResult> Import(IReadOnlyList<SpecificationAttributeDto> rows, bool dryRun,
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

    private async Task<ImportRowResult> ImportRow(int row, SpecificationAttributeDto dto, bool dryRun)
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
            errors.Add("Name is required for a new specification attribute.");
        if (dto.Name == "")
            errors.Add("Name cannot be empty.");

        errors.AddRange(_htmlGuard.PlainTextErrors((nameof(dto.Name), dto.Name), (nameof(dto.SeName), dto.SeName)));
        foreach (var option in dto.Options ?? [])
        {
            if (string.IsNullOrEmpty(option?.Name))
            {
                errors.Add("Option Name is required.");
                continue;
            }

            errors.AddRange(_htmlGuard.PlainTextErrors(("Option Name", option.Name),
                ("Option ColorSquaresRgb", option.ColorSquaresRgb)));
        }

        if (errors.Count > 0)
            return new ImportRowResult(row, ImportRowStatus.Rejected, existing?.Id ?? "", key, errors, warnings);

        if (dryRun)
            return new ImportRowResult(row, existing == null ? ImportRowStatus.Created : ImportRowStatus.Updated,
                existing?.Id ?? "", key, errors, warnings);

        var isNew = existing == null;
        var attribute = existing ?? new SpecificationAttribute { Name = dto.Name };
        if (!string.IsNullOrEmpty(dto.Name)) attribute.Name = dto.Name;
        if (dto.DisplayOrder.HasValue) attribute.DisplayOrder = dto.DisplayOrder.Value;
        if (isNew || !string.IsNullOrEmpty(dto.SeName))
            attribute.SeName = GetSeName(string.IsNullOrEmpty(dto.SeName) ? attribute.Name : dto.SeName);

        //a repeated name in the same row matches the option just added, so it collapses into it
        foreach (var optionDto in dto.Options ?? [])
        {
            var option = attribute.SpecificationAttributeOptions
                .FirstOrDefault(o => string.Equals(o.Name, optionDto.Name, StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                option = new SpecificationAttributeOption {
                    Name = optionDto.Name,
                    SeName = GetSeName(optionDto.Name)
                };
                attribute.SpecificationAttributeOptions.Add(option);
            }

            if (optionDto.ColorSquaresRgb != null) option.ColorSquaresRgb = optionDto.ColorSquaresRgb;
            if (optionDto.DisplayOrder.HasValue) option.DisplayOrder = optionDto.DisplayOrder.Value;
        }

        if (isNew) await _specificationAttributeService.InsertSpecificationAttribute(attribute);
        else await _specificationAttributeService.UpdateSpecificationAttribute(attribute);

        return new ImportRowResult(row, isNew ? ImportRowStatus.Created : ImportRowStatus.Updated, attribute.Id, key,
            errors, warnings);
    }

    private static string Key(SpecificationAttributeDto dto)
    {
        return !string.IsNullOrEmpty(dto.Name) ? dto.Name : !string.IsNullOrEmpty(dto.SeName) ? dto.SeName : dto.Id ?? "";
    }

    /// <summary>
    ///     By Id, else by SeName normalized as a new attribute's is, else (best effort) by the SeName its Name would get,
    ///     accepted only when the stored name is the same (any case)
    /// </summary>
    private async Task<(SpecificationAttribute attribute, bool matchedByName)> Find(SpecificationAttributeDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Id))
            return (await _specificationAttributeService.GetSpecificationAttributeById(dto.Id), false);

        if (!string.IsNullOrEmpty(dto.SeName))
            return (await FindBySeName(GetSeName(dto.SeName)), false);

        if (string.IsNullOrEmpty(dto.Name))
            return (null, false);

        var found = await FindBySeName(GetSeName(dto.Name));
        return found != null && string.Equals(found.Name, dto.Name, StringComparison.OrdinalIgnoreCase)
            ? (found, true)
            : (null, false);
    }

    private async Task<SpecificationAttribute> FindBySeName(string seName)
    {
        return string.IsNullOrEmpty(seName)
            ? null
            : await _specificationAttributeService.GetSpecificationAttributeBySeName(seName);
    }

    //same normalization as the panel's specification attribute controller
    private string GetSeName(string name)
    {
        return SeoExtensions.GetSeName(name, _seoSettings.ConvertNonWesternChars, _seoSettings.AllowUnicodeCharsInUrls,
            _seoSettings.SeoCharConversion);
    }
}
