using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.News;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.News
{
    public class NewsItemValidator : BaseGrandValidator<NewsItemModel>
    {
        public NewsItemValidator(
            IEnumerable<IValidatorConsumer<NewsItemModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.News.NewsItems.Fields.Title.Required"));
            RuleFor(x => x.Short).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.News.NewsItems.Fields.Short.Required"));
            RuleFor(x => x.Full).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.News.NewsItems.Fields.Full.Required"));
        }
    }
}