using Grand.Domain.Audit;

namespace Grand.Domain.PushNotifications;

public class PushMessage : BaseEntity
{
    [AuditName]
    public string Title { get; set; }

    public string Text { get; set; }

    public DateTime SentOn { get; set; }

    public int NumberOfReceivers { get; set; }
}