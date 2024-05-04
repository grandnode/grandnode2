using MediatR;

namespace Grand.Business.Core.Events.Marketing;

public class EmailUnsubscribedEvent : INotification
{
    public EmailUnsubscribedEvent(string email)
    {
        Email = email;
    }

    public string Email { get; }

    public bool Equals(EmailUnsubscribedEvent other)
    {
        if (ReferenceEquals(null, other))
            return false;
        return ReferenceEquals(this, other) || Equals(other.Email, Email);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return obj.GetType() == typeof(EmailUnsubscribedEvent) && Equals((EmailUnsubscribedEvent)obj);
    }

    public override int GetHashCode()
    {
        return Email != null ? Email.GetHashCode() : 0;
    }
}