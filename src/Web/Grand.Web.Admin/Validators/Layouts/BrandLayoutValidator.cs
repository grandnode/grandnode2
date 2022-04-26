using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Layouts;

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