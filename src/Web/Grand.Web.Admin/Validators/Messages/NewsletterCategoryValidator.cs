using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class NewsletterCategoryValidator : BaseGrandValidator<NewsletterCategoryModel>
    {
        public NewsletterCategoryValidator(
            IEnumerable<IValidatorConsumer<NewsletterCategoryModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.NewsletterCategory.Fields.Name.Required"));
        }
    }
}