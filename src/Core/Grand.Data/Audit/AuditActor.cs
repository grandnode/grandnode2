using Grand.Domain.Audit;

namespace Grand.Data.Audit;

/// <summary>
///     Who made the writes of the current request, pushed in by the web layer
/// </summary>
public record AuditActor(
    AuditSource Source,
    string CustomerId,
    string Email,
    string ImpersonatedCustomerId,
    string StoreId,
    string VendorId,
    string IpAddress,
    string UserAgent,
    string Action,
    string OperationId);
