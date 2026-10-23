using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Messages;

/// <summary>
///     Resolves what a message needs before it is sent: store, active template, email account and language
/// </summary>
public interface IMessageLookupService
{
    /// <summary>
    ///     Gets the store by identifier, or the first store when it does not exist
    /// </summary>
    Task<Store> GetStore(string storeId);

    /// <summary>
    ///     Gets an active message template by name, or null when it is missing or inactive
    /// </summary>
    Task<MessageTemplate> GetMessageTemplate(string messageTemplateName, string storeId);

    /// <summary>
    ///     Gets the email account of the message template, falling back to the default and then to any account
    /// </summary>
    Task<EmailAccount> GetEmailAccountOfMessageTemplate(MessageTemplate messageTemplate, string languageId);

    /// <summary>
    ///     Gets the language if published, else a published language of the store, else any language
    /// </summary>
    Task<Language> EnsureLanguageIsActive(string languageId, string storeId);
}
