namespace Grand.Data.Audit;

/// <summary>
///     Reads stored records as audit snapshots, to name the records changed fields refer to
/// </summary>
public interface IAuditRecordReader
{
    /// <summary>
    ///     The record of <paramref name="entityType" /> whose <paramref name="key" /> (the id when null) is
    ///     <paramref name="value" />, as "path → value", or null when there is none. With <paramref name="field" />,
    ///     the snapshot may hold only that top-level field.
    /// </summary>
    Task<IDictionary<string, string>> Read(Type entityType, string value, string key = null, string field = null);

    /// <summary>
    ///     The records whose <paramref name="key" /> is one of <paramref name="values" />, in one read, by that value; a
    ///     value without a record is left out. Otherwise as <see cref="Read" />.
    /// </summary>
    Task<IDictionary<string, IDictionary<string, string>>> ReadMany(Type entityType, IReadOnlyCollection<string> values,
        string key = null, string field = null);
}
