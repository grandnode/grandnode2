using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Common;

namespace Grand.Web.Admin.Validators.Common
{
    public class AddressAttributeValueValidator : BaseGrandValidator<AddressAttributeValueModel>
    {
        public AddressAttributeValueValidator(
            IEnumerable<IValidatorConsumer<AddressAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Address.AddressAttributes.Values.Fields.Name.Required"));
        }
    }
}