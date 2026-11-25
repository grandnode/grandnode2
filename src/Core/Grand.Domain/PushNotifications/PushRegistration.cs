namespace Grand.Domain.PushNotifications;

public class PushRegistration : BaseEntity
{
    public string CustomerId { get; set; } = null!;

    public bool Allowed { get; set; }

    public string? Token { get; set; }

    public DateTime RegisteredOn { get; set; }
}