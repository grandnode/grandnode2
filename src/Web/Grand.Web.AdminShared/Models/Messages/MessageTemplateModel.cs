using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.AdminShared.Models.Messages;

public class MessageTemplateModel : BaseEntityModel, ILocalizedModel<MessageTemplateLocalizedModel>, IStoreLinkModel
{
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

    public IList<EmailAccountModel> AvailableEmailAccounts { get; set; } = new List<EmailAccountModel>();
    public List<StoreModel> AvailableStores { get; set; }

    //comma-separated list of stores used on the list page
    [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.LimitedToStores")]
    public string ListOfStores { get; set; }

    public IList<MessageTemplateLocalizedModel> Locales { get; set; } = new List<MessageTemplateLocalizedModel>();

    //Store acl
    [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.LimitedToStores")]
    [UIHint("Stores")]
    public string[] Stores { get; set; }

    /// <summary>
    /// True when the caller can only preview the template (global, or shared with other
    /// stores), not save changes to it. Always false for Admin (unscoped, full CRUD on every
    /// template). For Store, mirrors <c>!AccessToEntityByStore(CurrentStoreId)</c> — set by
    /// <c>BaseMessageTemplateController.Edit(GET)</c>.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// True when the caller is allowed to invoke <c>CopyTemplate</c> on this entity. Always
    /// true for Admin (unrestricted copy, its original behavior). For Store, true exactly when
    /// the template is fully global (<c>LimitedToStores == false</c>) — deliberately NOT "not
    /// owned by me" (that is, NOT <c>!HasAccess</c>). A template exclusively owned by ANOTHER
    /// store also has <c>HasAccess == false</c>, so an ownership-based predicate would
    /// incorrectly mark another store's exclusive template as copyable and leak it. Set by
    /// <c>BaseMessageTemplateController.Edit(GET)</c>, mirroring <c>CopyTemplate</c>'s own
    /// guard exactly.
    /// </summary>
    public bool CanCopy { get; set; }
}

public class MessageTemplateLocalizedModel : ILocalizedModelLocal
{
    [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.BccEmailAddresses")]

    public string BccEmailAddresses { get; set; }

    [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Subject")]

    public string Subject { get; set; }

    [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.Body")]

    public string Body { get; set; }

    [GrandResourceDisplayName("Admin.Content.MessageTemplates.Fields.EmailAccount")]
    public string EmailAccountId { get; set; }

    public string LanguageId { get; set; }
}