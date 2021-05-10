using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Layouts;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Layouts
{
    public class CategoryLayoutValidator : BaseGrandValidator<CategoryLayoutModel>
    {
        public CategoryLayoutValidator(
            IEnumerable<IValidatorConsumer<CategoryLayoutModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Category.Name.Required"));
            RuleFor(x => x.ViewPath).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Category.ViewPath.Required"));
        }
    }
}