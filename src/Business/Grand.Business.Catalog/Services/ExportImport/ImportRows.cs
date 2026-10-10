using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Catalog.Services.ExportImport;

/// <summary>
///     Row: unknown Id rejected, matching by SeName/Sku/Name. Panel (XLSX): an unknown Id creates the entity with that Id.
/// </summary>
internal enum ImportMode
{
    Row,
    Panel
}

/// <summary>Helpers the catalogue row imports share</summary>
internal static class ImportRows
{
    /// <summary>
    ///     Runs the rows one by one. A cancelled token stops the batch at a row boundary: what was saved is reported, the
    ///     rest is not touched. With <paramref name="isolateFailures" /> a row that throws is logged and reported rejected
    ///     instead of losing the results of the rows saved before it; the panel's spreadsheet import leaves it off, so its
    ///     error still reaches the panel.
    /// </summary>
    public static async Task<ImportBatchResult> RunBatch<TDto>(IReadOnlyList<TDto> rows, bool dryRun,
        Func<TDto, string> id, Func<TDto, string> key, Func<int, TDto, Task<ImportRowResult>> importRow,
        bool isolateFailures, ILogger logger, CancellationToken cancellationToken)
    {
        var results = new List<ImportRowResult>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                results.Add(ImportRowResult.NotProcessed(i + 1, id(rows[i]), key(rows[i])));
                continue;
            }

            try
            {
                results.Add(await importRow(i + 1, rows[i]));
            }
            catch (Exception ex) when (isolateFailures)
            {
                logger.LogError(ex, "Import of row {Row} ({Key}) failed", i + 1, key(rows[i]));
                results.Add(ImportRowResult.Failed(i + 1, id(rows[i]), key(rows[i])));
            }
        }

        return new ImportBatchResult(dryRun, results);
    }

    /// <summary>
    ///     The entity a slug points to, when it is of <paramref name="entityName" />. A slug - active or a past one - is
    ///     reserved for one entity (ISeNameService.ValidateSeName), so it never resolves to two.
    /// </summary>
    public static async Task<T> FindBySlug<T>(ISlugService slugService, string slug, string entityName,
        Func<string, Task<T>> getById) where T : class
    {
        var url = await slugService.GetBySlug(slug);
        if (url == null || url.EntityName != entityName || string.IsNullOrEmpty(url.EntityId))
            return null;

        return await getById(url.EntityId);
    }

    /// <summary>
    ///     The one candidate whose stored name is the row's name (any case). The candidates come from the service's
    ///     "name contains" filter, so a de-duplicated slug (apple-2) does not hide the entity. More than one is an error:
    ///     the row has to name the entity by Id or SeName.
    /// </summary>
    public static (T match, string error) SingleByName<T>(IEnumerable<T> candidates, Func<T, string> name,
        string wanted, string entity) where T : class
    {
        var matches = candidates.Where(c => string.Equals(name(c), wanted, StringComparison.OrdinalIgnoreCase))
            .Take(2).ToList();
        return matches.Count switch {
            0 => (null, null),
            1 => (matches[0], null),
            _ => (null, $"Name '{wanted}' matches more than one {entity}; send Id or SeName.")
        };
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
