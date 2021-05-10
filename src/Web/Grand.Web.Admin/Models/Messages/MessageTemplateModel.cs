using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class MessageTemplateModel : BaseEntityModel, ILocalizedModel<MessageTemplateLocalizedModel>, IStoreLinkModel
    {
        public MessageTemplateModel()
        {
            Locales = new List<MessageTemplateLocalizedModel>();
            AvailableEmailAccounts = new List<EmailAccountModel>();
        }


        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.AllowedTokens")]
        public string[] AllowedTokens { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.BccEmailAddresses")]

        public string BccEmailAddresses { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Subject")]

        public string Subject { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Body")]

        public string Body { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.IsActive")]

        public bool IsActive { get; set; }
        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.SendImmediately")]
        public bool SendImmediately { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.DelayBeforeSend")]
        [UIHint("Int32Nullable")]
        public int? DelayBeforeSend { get; set; }
        public int DelayPeriodId { get; set; }

        public bool HasAttachedDownload { get; set; }
        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.AttachedDownload")]
        [UIHint("Download")]
        public string AttachedDownloadId { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.EmailAccount")]
        public string EmailAccountId { get; set; }
        public IList<EmailAccountModel> AvailableEmailAccounts { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }
        public List<StoreModel> AvailableStores { get; set; }

        //comma-separated list of stores used on the list page
        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.LimitedToStores")]
        public string ListOfStores { get; set; }

        public IList<MessageTemplateLocalizedModel> Locales { get; set; }
    }

    public partial class MessageTemplateLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.BccEmailAddresses")]

        public string BccEmailAddresses { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Subject")]

        public string Subject { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Body")]

        public string Body { get; set; }

        [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.EmailAccount")]
        public string EmailAccountId { get; set; }
    }
}