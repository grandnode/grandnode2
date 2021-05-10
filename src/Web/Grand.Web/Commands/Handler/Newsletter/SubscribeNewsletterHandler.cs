using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Messages;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Newsletter;
using Grand.Web.Models.Newsletter;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Newsletter
{
    public class SubscribeNewsletterHandler : IRequestHandler<SubscribeNewsletterCommand, SubscribeNewsletterResultModel>
    {
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ITranslationService _translationService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IWorkContext _workContext;
        private readonly INewsletterCategoryService _newsletterCategoryService;


        public SubscribeNewsletterHandler(INewsLetterSubscriptionService newsLetterSubscriptionService, ITranslationService translationService,
            IMessageProviderService messageProviderService, IWorkContext workContext, INewsletterCategoryService newsletterCategoryService)
        {
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _translationService = translationService;
            _messageProviderService = messageProviderService;
            _workContext = workContext;
            _newsletterCategoryService = newsletterCategoryService;
        }

        public async Task<SubscribeNewsletterResultModel> Handle(SubscribeNewsletterCommand request, CancellationToken cancellationToken)
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

                var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(email, _workContext.CurrentStore.Id);
                if (subscription != null)
                {
                    if (request.Subscribe)
                    {
                        if (!subscription.Active)
                        {
                            await _messageProviderService.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }
                        model.Result = _translationService.GetResource("Newsletter.SubscribeEmailSent");
                    }
                    else
                    {
                        if (subscription.Active)
                        {
                            await _messageProviderService.SendNewsLetterSubscriptionDeactivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }
                        model.Result = _translationService.GetResource("Newsletter.UnsubscribeEmailSent");
                    }
                }
                else if (request.Subscribe)
                {
                    subscription = new NewsLetterSubscription
                    {
                        NewsLetterSubscriptionGuid = Guid.NewGuid(),
                        Email = email,
                        CustomerId = _workContext.CurrentCustomer.Id,
                        Active = false,
                        StoreId = _workContext.CurrentStore.Id,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    await _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);

                    await _messageProviderService.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);

                    model.Result = _translationService.GetResource("Newsletter.SubscribeEmailSent");
                    var modelCategory = await PrepareNewsletterCategory(subscription.Id);
                    if (modelCategory.NewsletterCategories.Count > 0)
                    {
                        model.NewsletterCategory = modelCategory;
                    }
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
            var model = new NewsletterCategoryModel();
            model.NewsletterEmailId = id;
            var categories = await _newsletterCategoryService.GetNewsletterCategoriesByStore(_workContext.CurrentStore.Id);
            foreach (var item in categories)
            {
                model.NewsletterCategories.Add(new NewsletterSimpleCategory()
                {
                    Id = item.Id,
                    Name = item.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                    Description = item.GetTranslation(x => x.Description, _workContext.WorkingLanguage.Id),
                    Selected = item.Selected
                });
            }
            return model;
        }

    }
}
