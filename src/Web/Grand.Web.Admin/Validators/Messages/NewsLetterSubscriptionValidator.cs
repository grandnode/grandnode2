﻿using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Validators.Messages
{
    public class NewsLetterSubscriptionValidator : BaseGrandValidator<NewsLetterSubscriptionModel>
    {
        public NewsLetterSubscriptionValidator(
            IEnumerable<IValidatorConsumer<NewsLetterSubscriptionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.NewsLetterSubscriptions.Fields.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Admin.Common.WrongEmail"));
        }
    }
}