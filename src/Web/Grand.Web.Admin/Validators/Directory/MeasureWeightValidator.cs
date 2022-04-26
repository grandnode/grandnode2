using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Validators.Directory
{
    public class MeasureWeightValidator : BaseGrandValidator<MeasureWeightModel>
    {
        public MeasureWeightValidator(
            IEnumerable<IValidatorConsumer<MeasureWeightModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Measures.Weights.Fields.Name.Required"));
            RuleFor(x => x.SystemKeyword).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Measures.Weights.Fields.SystemKeyword.Required"));
        }
    }
}