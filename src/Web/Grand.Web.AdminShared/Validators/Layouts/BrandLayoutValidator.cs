using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Layouts;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Layouts;

public class BrandLayoutValidator : BaseGrandValidator<BrandLayoutModel>
{
    public BrandLayoutValidator(
        IEnumerable<IValidatorConsumer<BrandLayoutModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Brand.Name.Required"));
        RuleFor(x => x.ViewPath).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Layouts.Brand.ViewPath.Required"));
    }
}