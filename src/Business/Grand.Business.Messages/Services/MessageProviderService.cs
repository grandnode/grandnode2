using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Messages;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Messages.Services;

public class MessageProviderService : IMessageProviderService
{
    #region Ctor

    public MessageProviderService(IQueuedEmailService queuedEmailService)
    {
        _queuedEmailService = queuedEmailService;
    }

    #endregion

    #region Fields

    private readonly IQueuedEmailService _queuedEmailService;

    #endregion

    #region Methods

    public virtual async Task<int> SendNotification(MessageTemplate messageTemplate,
        EmailAccount emailAccount, string languageId, LiquidObject liquidObject,
        string toEmailAddress, string toName,
        string attachmentFilePath = null, string attachmentFileName = null,
        IEnumerable<string> attachedDownloads = null,
        string replyToEmailAddress = null, string replyToName = null,
        string fromEmail = null, string fromName = null, string subject = null,
        Reference reference = Reference.None, string objectId = "", string invoiceDownloadId = null)
    {
        if (string.IsNullOrEmpty(toEmailAddress))
            return 0;

        //retrieve translation message template data
        var bcc = messageTemplate.GetTranslation(mt => mt.BccEmailAddresses, languageId);

        if (string.IsNullOrEmpty(subject))
            subject = messageTemplate.GetTranslation(mt => mt.Subject, languageId);

        var body = messageTemplate.GetTranslation(mt => mt.Body, languageId);

        var email = new QueuedEmail();
        liquidObject.Email = new LiquidEmail(email.Id);

        var subjectReplaced = LiquidExtensions.Render(liquidObject, subject);
        var bodyReplaced = LiquidExtensions.Render(liquidObject, body);

        var attachments = new List<string>();
        if (attachedDownloads != null)
            attachments.AddRange(attachedDownloads);
        if (!string.IsNullOrEmpty(messageTemplate.AttachedDownloadId))
            attachments.Add(messageTemplate.AttachedDownloadId);

        //limit name length
        toName = CommonHelper.EnsureMaximumLength(toName, 300);
        email.PriorityId = QueuedEmailPriority.High;
        email.From = !string.IsNullOrEmpty(fromEmail) ? fromEmail : emailAccount.Email;
        email.FromName = !string.IsNullOrEmpty(fromName) ? fromName : emailAccount.DisplayName;
        email.To = toEmailAddress;
        email.ToName = toName;
        email.ReplyTo = replyToEmailAddress;
        email.ReplyToName = replyToName;
        email.CC = string.Empty;
        email.Bcc = bcc;
        email.Subject = subjectReplaced;
        email.Body = bodyReplaced;
        email.AttachmentFilePath = attachmentFilePath;
        email.AttachmentFileName = attachmentFileName;
        email.AttachedDownloads = attachments;
        email.InvoiceDownloadId = invoiceDownloadId;
        email.EmailAccountId = emailAccount.Id;
        email.DontSendBeforeDateUtc = !messageTemplate.DelayBeforeSend.HasValue
            ? null
            : DateTime.UtcNow +
              TimeSpan.FromHours(messageTemplate.DelayPeriodId.ToHours(messageTemplate.DelayBeforeSend.Value));
        email.Reference = reference;
        email.ObjectId = objectId;

        await _queuedEmailService.InsertQueuedEmail(email);
        return 1;
    }

    #endregion
}
