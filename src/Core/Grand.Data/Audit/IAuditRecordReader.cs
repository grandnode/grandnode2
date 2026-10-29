namespace Grand.Data.Audit;

/// <summary>
///     Reads one stored record as an audit snapshot, to name the record a changed field refers to
/// </summary>
public interface IAuditRecordReader
{
    /// <summary>
    ///     The record of <paramref name="entityType" /> whose <paramref name="key" /> (the id when null) is
    ///     <paramref name="value" />, as "path → value", or null when there is none. With <paramref name="field" />,
    ///     the snapshot may hold only that top-level field.
    /// </summary>
    Task<IDictionary<string, string>> Read(Type entityType, string value, string key = null, string field = null);
}
