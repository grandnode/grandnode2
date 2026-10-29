namespace Grand.Domain.Audit;

/// <summary>
///     Marks a property that refers to another record (or holds an enum value as a number), so the audit log shows
///     "name (id)" instead of a bare id. <paramref name="target" /> is the referenced entity or the enum;
///     <paramref name="list" /> names the list inside the target that holds the referenced element, whose owning
///     target record is given by the sibling property named in <see cref="Owner" />.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditReferenceAttribute(Type target, string list = null) : Attribute
{
    public Type Target { get; } = target;

    public string List { get; } = list;

    /// <summary>
    ///     The sibling property holding the id of the target record that contains <see cref="List" />
    /// </summary>
    public string Owner { get; set; }
}
