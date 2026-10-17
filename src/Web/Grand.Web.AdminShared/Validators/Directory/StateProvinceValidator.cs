using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Directory;

public class StateProvinceValidator : BaseGrandValidator<StateProvinceModel>
{
    public StateProvinceValidator(
        IEnumerable<IValidatorConsumer<StateProvinceModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Countries.States.Fields.Name.Required"));
    }
}