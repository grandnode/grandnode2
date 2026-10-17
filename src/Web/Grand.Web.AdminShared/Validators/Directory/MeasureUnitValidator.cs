using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Directory;

public class MeasureUnitValidator : BaseGrandValidator<MeasureUnitModel>
{
    public MeasureUnitValidator(
        IEnumerable<IValidatorConsumer<MeasureUnitModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Measures.Units.Fields.Name.Required"));
    }
}