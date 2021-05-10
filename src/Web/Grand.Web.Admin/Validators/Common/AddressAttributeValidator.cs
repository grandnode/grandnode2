using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Common;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Common
{
    public class AddressAttributeValidator : BaseGrandValidator<AddressAttributeModel>
    {
        public AddressAttributeValidator(
            IEnumerable<IValidatorConsumer<AddressAttributeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Address.AddressAttributes.Fields.Name.Required"));
        }
    }
}