using Grand.Domain;

namespace Grand.Web.Admin.Models.PushNotifications
{
    public class PushMessageGridModel : BaseEntity
    {
        public string Title { get; set; }

        public string Text { get; set; }

        public DateTime SentOn { get; set; }

        public int NumberOfReceivers { get; set; }
    }
}
