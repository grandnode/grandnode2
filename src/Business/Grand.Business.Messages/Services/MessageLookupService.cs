using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Messages.Services;

public class MessageLookupService : IMessageLookupService
{
    #region Ctor

    public MessageLookupService(
        IMessageTemplateService messageTemplateService,
        ILanguageService languageService,
        IEmailAccountService emailAccountService,
        IStoreService storeService,
        EmailAccountSettings emailAccountSettings)
    {
        _messageTemplateService = messageTemplateService;
        _languageService = languageService;
        _emailAccountService = emailAccountService;
        _storeService = storeService;
        _emailAccountSettings = emailAccountSettings;
    }

    #endregion

    #region Fields

    private readonly IMessageTemplateService _messageTemplateService;
    private readonly ILanguageService _languageService;
    private readonly IEmailAccountService _emailAccountService;
    private readonly IStoreService _storeService;
    private readonly EmailAccountSettings _emailAccountSettings;

    #endregion

    #region Methods

    public virtual async Task<Store> GetStore(string storeId)
    {
        return await _storeService.GetStoreById(storeId) ?? (await _storeService.GetAllStores()).FirstOrDefault();
    }

    public virtual async Task<MessageTemplate> GetMessageTemplate(string messageTemplateName, string storeId)
    {
        var messageTemplate = await _messageTemplateService.GetMessageTemplateByName(messageTemplateName, storeId);

        //no template found
        if (messageTemplate == null)
            return null;

        //ensure it's active
        var isActive = messageTemplate.IsActive;
        return !isActive ? null : messageTemplate;
    }

    public virtual async Task<EmailAccount> GetEmailAccountOfMessageTemplate(MessageTemplate messageTemplate,
        string languageId)
    {
        var emailAccounId = messageTemplate.GetTranslation(mt => mt.EmailAccountId, languageId);
        var emailAccount = await _emailAccountService.GetEmailAccountById(emailAccounId);
        emailAccount ??= await _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
        emailAccount ??= (await _emailAccountService.GetAllEmailAccounts()).FirstOrDefault();
        return emailAccount;
    }

    public virtual async Task<Language> EnsureLanguageIsActive(string languageId, string storeId)
    {
        //load language by specified ID
        var language = await _languageService.GetLanguageById(languageId);

        if (language is not { Published: true })
            //load any language from the specified store
            language = (await _languageService.GetAllLanguages(storeId: storeId)).FirstOrDefault();
        if (language is not { Published: true })
            //load any language
            language = (await _languageService.GetAllLanguages()).FirstOrDefault();

        if (language == null)
            throw new Exception("No active language could be loaded");
        return language;
    }

    #endregion
}
