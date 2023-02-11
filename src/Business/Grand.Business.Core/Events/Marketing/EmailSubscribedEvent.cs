using MediatR;

namespace Grand.Business.Core.Events.Marketing
{
    public class EmailSubscribedEvent : INotification
    {
        private readonly string _email;

        public EmailSubscribedEvent(string email)
        {
            _email = email;
        }

        public string Email => _email;

        public bool Equals(EmailSubscribedEvent other)
        {
            if (ReferenceEquals(null, other))
                return false;
            return ReferenceEquals(this, other) || Equals(other._email, _email);
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
            return _email != null ? _email.GetHashCode() : 0;
        }
    }
}
