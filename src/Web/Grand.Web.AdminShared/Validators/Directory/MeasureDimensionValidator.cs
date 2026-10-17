using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Directory;

public class MeasureDimensionValidator : BaseGrandValidator<MeasureDimensionModel>
{
    public MeasureDimensionValidator(
        IEnumerable<IValidatorConsumer<MeasureDimensionModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Measures.Dimensions.Fields.Name.Required"));
        RuleFor(x => x.SystemKeyword).NotEmpty().WithMessage(
            translationService.GetResource("Admin.Configuration.Measures.Dimensions.Fields.SystemKeyword.Required"));
    }
}