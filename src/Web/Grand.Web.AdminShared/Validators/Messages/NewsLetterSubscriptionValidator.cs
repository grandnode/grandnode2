using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Messages;

public class NewsLetterSubscriptionValidator : BaseGrandValidator<NewsLetterSubscriptionModel>
{
    public NewsLetterSubscriptionValidator(
        IEnumerable<IValidatorConsumer<NewsLetterSubscriptionModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(
                translationService.GetResource("admin.marketing.NewsLetterSubscriptions.Fields.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Admin.Common.WrongEmail"));
    }
}