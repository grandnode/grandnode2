using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Pages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Pages
{
    public class PageValidator : BaseGrandValidator<PageModel>
    {
        public PageValidator(
            IEnumerable<IValidatorConsumer<PageModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.SystemName).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.Pages.Fields.SystemName.Required"));
        }
    }
}