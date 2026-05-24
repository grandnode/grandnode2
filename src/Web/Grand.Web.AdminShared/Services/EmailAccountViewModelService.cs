using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class EmailAccountViewModelService : IEmailAccountViewModelService
{
    private readonly IEmailAccountService _emailAccountService;
    private readonly IEmailSender _emailSender;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;

    public EmailAccountViewModelService(IEmailAccountService emailAccountService, IEmailSender emailSender,
        IStoreService storeService, ITranslationService translationService)
    {
        _emailAccountService = emailAccountService;
        _emailSender = emailSender;
        _storeService = storeService;
        _translationService = translationService;
    }

    public virtual async Task<EmailAccountModel> PrepareEmailAccountModel()
    {
        var model = new EmailAccountModel {
            //default values
            Port = 25
        };
        await PopulateAvailableStores(model);
        return model;
    }

    public virtual async Task PrepareAvailableStores(EmailAccountModel model)
    {
        await PopulateAvailableStores(model);
    }

    public virtual async Task<EmailAccount> InsertEmailAccountModel(EmailAccountModel model)
    {
        var emailAccount = model.ToEntity();
        //set password manually
        emailAccount.Password = model.Password;
        await _emailAccountService.InsertEmailAccount(emailAccount);
        return emailAccount;
    }

    public virtual async Task<EmailAccount> UpdateEmailAccountModel(EmailAccount emailAccount, EmailAccountModel model)
    {
        emailAccount = model.ToEntity(emailAccount);
        if (!string.IsNullOrEmpty(model.Password))
            emailAccount.Password = model.Password;

        await _emailAccountService.UpdateEmailAccount(emailAccount);
        return emailAccount;
    }

    public virtual async Task SendTestEmail(EmailAccount emailAccount, EmailAccountModel model)
    {
        var subject = "Testing email functionality.";
        var body = "Email works fine.";
        await _emailSender.SendEmail(emailAccount, subject, body, emailAccount.Email, emailAccount.DisplayName,
            model.SendTestEmailTo, null);
    }

    private async Task PopulateAvailableStores(EmailAccountModel model)
    {
        model.AvailableStores.Add(new SelectListItem {
            Value = "",
            Text = _translationService.GetResource("Admin.Settings.StoreScope.AllStores")
        });
        foreach (var store in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem {
                Value = store.Id,
                Text = store.Name
            });
    }
}