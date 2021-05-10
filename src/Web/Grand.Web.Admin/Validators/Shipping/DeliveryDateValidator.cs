using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Shipping;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Shipping
{
    public class DeliveryDateValidator : BaseGrandValidator<DeliveryDateModel>
    {
        public DeliveryDateValidator(
            IEnumerable<IValidatorConsumer<DeliveryDateModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Shipping.DeliveryDates.Fields.Name.Required"));
        }
    }
}