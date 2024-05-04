using MediatR;

namespace Grand.Business.Core.Events.Marketing;

public class EmailSubscribedEvent : INotification
{
    public EmailSubscribedEvent(string email)
    {
        Email = email;
    }

    public string Email { get; }

    public bool Equals(EmailSubscribedEvent other)
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
        return obj.GetType() == typeof(EmailSubscribedEvent) && Equals((EmailSubscribedEvent)obj);
    }

    public override int GetHashCode()
    {
        return Email != null ? Email.GetHashCode() : 0;
    }
}