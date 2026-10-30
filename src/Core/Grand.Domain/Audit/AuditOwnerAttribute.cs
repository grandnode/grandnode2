namespace Grand.Domain.Audit;

/// <summary>
///     Marks the property holding the id of the record this one belongs to (the order of an order note), so the
///     entries of this record also show in that record's change history. The property carries
///     [AuditReference] naming the owner's type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditOwnerAttribute : Attribute;
