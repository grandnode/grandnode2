using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Directory;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Directory
{
    public class MeasureDimensionValidator : BaseGrandValidator<MeasureDimensionModel>
    {
        public MeasureDimensionValidator(
            IEnumerable<IValidatorConsumer<MeasureDimensionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Measures.Dimensions.Fields.Name.Required"));
            RuleFor(x => x.SystemKeyword).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Measures.Dimensions.Fields.SystemKeyword.Required"));
        }
    }
}