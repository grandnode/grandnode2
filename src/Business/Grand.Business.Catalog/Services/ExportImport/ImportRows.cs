using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Seo;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>Helpers the catalogue row imports share</summary>
internal static class ImportRows
{
    /// <summary>The slug a new category, brand or collection gets from its name (as ISeNameService.ValidateSeName builds it)</summary>
    public static string NameSlug(string name, SeoSettings seoSettings)
    {
        var slug = SeoExtensions.GenerateSlug(name, seoSettings.ConvertNonWesternChars,
            seoSettings.AllowUnicodeCharsInUrls, seoSettings.AllowSlashChar, seoSettings.SeoCharConversion);
        return CommonHelper.EnsureMaximumLength(slug, 200);
    }

    public static string MatchedByNameWarning(string name)
    {
        return $"Matched existing '{name}' by name.";
    }

    /// <summary>The panel's spreadsheet import has no per-row result to show; its rejected rows go to the log</summary>
    public static void LogRejected(ILogger logger, string entity, ImportBatchResult result)
    {
        var rejected = result.Rows.Where(r => r.Status == ImportRowStatus.Rejected).ToList();
        if (rejected.Count == 0)
            return;

        logger.LogWarning("The {Entity} spreadsheet import skipped {Count} row(s): {Rows}", entity, rejected.Count,
            string.Join("; ", rejected.Select(r => $"row {r.Row} ({r.Key}): {string.Join(" ", r.Errors)}")));
    }
}
