using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Settings;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Settings
{
    public class MerchandiseReturnActionValidator : BaseGrandValidator<MerchandiseReturnActionModel>
    {
        public MerchandiseReturnActionValidator(
            IEnumerable<IValidatorConsumer<MerchandiseReturnActionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Settings.Order.MerchandiseReturnActions.Name.Required"));
        }
    }
}