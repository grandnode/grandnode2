using Grand.Domain.Audit;

namespace Grand.Domain.Stores;

public class DomainHost : SubBaseEntity
{
    [AuditName]
    public string? HostName { get; set; }
    public string Url { get; set; } = null!;
    public bool Primary { get; set; }
}