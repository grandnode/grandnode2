namespace Grand.Domain.Audit;

/// <summary>
///     Marks the property that names a record in the audit log (shown in the list, and the only trace of what a
///     deleted record was). Without it, the record's Name property is used.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditNameAttribute : Attribute;
