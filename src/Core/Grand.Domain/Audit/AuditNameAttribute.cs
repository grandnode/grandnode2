namespace Grand.Domain.Audit;

/// <summary>
///     Marks the property that names a record in the audit log (shown in the list, and the only trace of what a
///     deleted record was), or an element of a record's list. Without it, the Name property is used. On an
///     [AuditReference] property, the name is the referenced record's.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditNameAttribute : Attribute;
