using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Directory;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Directory
{
    public class StateProvinceValidator : BaseGrandValidator<StateProvinceModel>
    {
        public StateProvinceValidator(
            IEnumerable<IValidatorConsumer<StateProvinceModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Countries.States.Fields.Name.Required"));
        }
    }
}