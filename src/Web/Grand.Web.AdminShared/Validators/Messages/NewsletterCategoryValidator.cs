using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Messages;

public class NewsletterCategoryValidator : BaseGrandValidator<NewsletterCategoryModel>
{
    public NewsletterCategoryValidator(
        IEnumerable<IValidatorConsumer<NewsletterCategoryModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("admin.marketing.NewsletterCategory.Fields.Name.Required"));
    }
}