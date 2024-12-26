﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Newsletter;
using Grand.Web.Models.Newsletter;
using MediatR;

namespace Grand.Web.Commands.Handler.Newsletter;

public class SubscribeNewsletterHandler : IRequestHandler<SubscribeNewsletterCommand, SubscribeNewsletterResultModel>
{
    private readonly IMessageProviderService _messageProviderService;
    private readonly INewsletterCategoryService _newsletterCategoryService;
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;


    public SubscribeNewsletterHandler(INewsLetterSubscriptionService newsLetterSubscriptionService,
        ITranslationService translationService,
        IMessageProviderService messageProviderService, IWorkContextAccessor workContextAccessor,
        INewsletterCategoryService newsletterCategoryService)
    {
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _translationService = translationService;
        _messageProviderService = messageProviderService;
        _workContextAccessor = workContextAccessor;
        _newsletterCategoryService = newsletterCategoryService;
    }

    public async Task<SubscribeNewsletterResultModel> Handle(SubscribeNewsletterCommand request,
        CancellationToken cancellationToken)
    {
        var model = new SubscribeNewsletterResultModel();
        var email = request.Email;

        if (!CommonHelper.IsValidEmail(email))
        {
            model.Result = _translationService.GetResource("Newsletter.Email.Wrong");
        }
        else
        {
            email = email.Trim();

            var subscription =
                await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(email,
                    _workContextAccessor.WorkContext.CurrentStore.Id);
            if (subscription != null)
            {
                if (request.Subscribe)
                {
                    if (!subscription.Active)
                        await _messageProviderService.SendNewsLetterSubscriptionActivationMessage(subscription,
                            _workContextAccessor.WorkContext.WorkingLanguage.Id);
                    model.Result = _translationService.GetResource("Newsletter.SubscribeEmailSent");
                }
                else
                {
                    if (subscription.Active)
                        await _messageProviderService.SendNewsLetterSubscriptionDeactivationMessage(subscription,
                            _workContextAccessor.WorkContext.WorkingLanguage.Id);
                    model.Result = _translationService.GetResource("Newsletter.UnsubscribeEmailSent");
                }
            }
            else if (request.Subscribe)
            {
                subscription = new NewsLetterSubscription {
                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                    Email = email,
                    CustomerId = _workContextAccessor.WorkContext.CurrentCustomer.Id,
                    Active = false,
                    StoreId = _workContextAccessor.WorkContext.CurrentStore.Id
                };
                await _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);

                await _messageProviderService.SendNewsLetterSubscriptionActivationMessage(subscription,
                    _workContextAccessor.WorkContext.WorkingLanguage.Id);

                model.Result = _translationService.GetResource("Newsletter.SubscribeEmailSent");
                var modelCategory = await PrepareNewsletterCategory(subscription.Id);
                if (modelCategory.NewsletterCategories.Count > 0) model.NewsletterCategory = modelCategory;
            }
            else
            {
                model.Result = _translationService.GetResource("Newsletter.UnsubscribeEmailSent");
            }

            model.Success = true;
        }

        return model;
    }

    private async Task<NewsletterCategoryModel> PrepareNewsletterCategory(string id)
    {
        var model = new NewsletterCategoryModel {
            NewsletterEmailId = id
        };
        var categories = await _newsletterCategoryService.GetNewsletterCategoriesByStore(_workContextAccessor.WorkContext.CurrentStore.Id);
        foreach (var item in categories)
            model.NewsletterCategories.Add(new NewsletterSimpleCategory {
                Id = item.Id,
                Name = item.GetTranslation(x => x.Name, _workContextAccessor.WorkContext.WorkingLanguage.Id),
                Description = item.GetTranslation(x => x.Description, _workContextAccessor.WorkContext.WorkingLanguage.Id),
                Selected = item.Selected
            });
        return model;
    }
}