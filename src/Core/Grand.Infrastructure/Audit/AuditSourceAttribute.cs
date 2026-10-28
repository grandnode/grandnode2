using Grand.Domain.Audit;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Marks controllers outside the panel areas whose staff requests are audited (API, SCIM)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class AuditSourceAttribute(AuditSource source) : Attribute
{
    public AuditSource Source { get; } = source;
}
