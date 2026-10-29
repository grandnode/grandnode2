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
    string OperationId)
{
    public void Stamp(AuditEntry entry)
    {
        entry.Source = Source;
        entry.ActorCustomerId = CustomerId;
        entry.ActorEmail = Email;
        entry.ImpersonatedCustomerId = ImpersonatedCustomerId;
        entry.StoreId = StoreId;
        entry.VendorId = VendorId;
        entry.IpAddress = IpAddress;
        entry.UserAgent = UserAgent;
        entry.Action = Action;
        entry.OperationId = OperationId;
    }
}
