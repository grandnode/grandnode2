using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class NewsLetterSubscriptionModel : BaseEntityModel
    {
        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.Fields.Email")]
        public string Email { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.Fields.Active")]
        public bool Active { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.Fields.Store")]
        public string StoreName { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.Fields.Categories")]
        public string Categories { get; set; }

        [GrandResourceDisplayName("admin.marketing.NewsLetterSubscriptions.Fields.CreatedOn")]
        public string CreatedOn { get; set; }
    }
}