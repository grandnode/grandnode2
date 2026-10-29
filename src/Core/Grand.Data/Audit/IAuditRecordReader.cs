namespace Grand.Data.Audit;

/// <summary>
///     Reads one stored record as an audit snapshot, to name the record a changed field refers to
/// </summary>
public interface IAuditRecordReader
{
    /// <summary>
    ///     The record of <paramref name="entityType" /> with the id as "path → value", or null when there is none
    /// </summary>
    Task<IDictionary<string, string>> Read(Type entityType, string id);
}
