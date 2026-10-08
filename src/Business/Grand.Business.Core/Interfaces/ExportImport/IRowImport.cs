namespace Grand.Business.Core.Interfaces.ExportImport;

public enum ImportRowStatus
{
    Created,
    Updated,
    Rejected
}

/// <param name="Row">1-based position of the row in the submitted batch</param>
/// <param name="Id">Identifier of the created or updated entity; empty for a rejected new row, or a dry-run new row without an Id</param>
/// <param name="Key">Human-readable key of the row (name, else SeName, else id)</param>
public sealed record ImportRowResult(int Row, ImportRowStatus Status, string Id, string Key,
    IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings)
{
    public const string NotProcessedError = "Not processed: the batch time limit was reached; send these rows again.";

    /// <summary>A row the import did not reach because its cancellation token fired; nothing was written for it</summary>
    public static ImportRowResult NotProcessed(int row, string id, string key)
    {
        return new ImportRowResult(row, ImportRowStatus.Rejected, id ?? "", key, [NotProcessedError], []);
    }
}

public sealed record ImportBatchResult(bool DryRun, IReadOnlyList<ImportRowResult> Rows)
{
    public int Created => Rows.Count(r => r.Status == ImportRowStatus.Created);
    public int Updated => Rows.Count(r => r.Status == ImportRowStatus.Updated);
    public int Rejected => Rows.Count(r => r.Status == ImportRowStatus.Rejected);
}

/// <summary>
///     Row-level import with validation and dry run; the panel's Execute runs it with dryRun = false. A cancelled
///     token does not throw: the rows not reached yet come back rejected with <see cref="ImportRowResult.NotProcessedError" />
/// </summary>
public interface IRowImport<in TDto>
{
    Task<ImportBatchResult> Import(IReadOnlyList<TDto> rows, bool dryRun, CancellationToken cancellationToken = default);
}
