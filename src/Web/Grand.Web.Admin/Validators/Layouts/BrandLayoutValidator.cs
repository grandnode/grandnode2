using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Layouts;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Layouts
{
    public class BrandLayoutValidator : BaseGrandValidator<BrandLayoutModel>
    {
        public BrandLayoutValidator(
            IEnumerable<IValidatorConsumer<BrandLayoutModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Brand.Name.Required"));
            RuleFor(x => x.ViewPath).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Brand.ViewPath.Required"));
        }
    }
}