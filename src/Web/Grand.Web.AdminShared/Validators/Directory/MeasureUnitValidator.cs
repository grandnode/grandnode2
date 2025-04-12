using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Validators.Directory;

public class MeasureUnitValidator : BaseGrandValidator<MeasureUnitModel>
{
    public MeasureUnitValidator(
        IEnumerable<IValidatorConsumer<MeasureUnitModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Measures.Units.Fields.Name.Required"));
    }
}