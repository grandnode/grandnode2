using Grand.Domain.Audit;
using Grand.Mediator;

namespace Grand.Infrastructure.Audit;

/// <summary>
///     Published after an audit entry is stored, so a plugin can forward it (e.g. to a SIEM)
/// </summary>
public class AuditEntryCreated(AuditEntry entry) : INotification
{
    public AuditEntry Entry { get; } = entry;
}
