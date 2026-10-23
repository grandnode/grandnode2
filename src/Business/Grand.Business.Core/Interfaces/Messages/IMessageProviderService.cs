using Grand.Domain.Common;
using Grand.Domain.Messages;

namespace Grand.Business.Core.Interfaces.Messages;

public interface IMessageProviderService
{
    /// <summary>
    ///     Send notification
    /// </summary>
    /// <param name="messageTemplate">Message template</param>
    /// <param name="emailAccount">Email account</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="liquidObject">LiquidObject</param>
    /// <param name="toEmailAddress">Recipient email address</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="attachmentFilePath">Attachment file path</param>
    /// <param name="attachmentFileName">Attachment file name</param>
    /// <param name="attachedDownloads">Attached downloads ident</param>
    /// <param name="replyToEmailAddress">"Reply to" email</param>
    /// <param name="replyToName">"Reply to" name</param>
    /// <param name="fromEmail">Sender email. If specified, then it overrides passed "emailAccount" details</param>
    /// <param name="fromName">Sender name. If specified, then it overrides passed "emailAccount" details</param>
    /// <param name="subject">Subject. If specified, then it overrides subject of a message template</param>
    /// <param name="reference">Reference object</param>
    /// <param name="objectId">Reference object ident</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendNotification(MessageTemplate messageTemplate,
        EmailAccount emailAccount, string languageId, LiquidObject liquidObject,
        string toEmailAddress, string toName,
        string attachmentFilePath = null, string attachmentFileName = null,
        IEnumerable<string> attachedDownloads = null,
        string replyToEmailAddress = null, string replyToName = null,
        string fromEmail = null, string fromName = null, string subject = null,
        Reference reference = Reference.None, string objectId = "");
}
