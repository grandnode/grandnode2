using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Orders;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Orders
{
    public class CheckoutAttributeValueValidator : BaseGrandValidator<CheckoutAttributeValueModel>
    {
        public CheckoutAttributeValueValidator(
            IEnumerable<IValidatorConsumer<CheckoutAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Orders.CheckoutAttributes.Values.Fields.Name.Required"));
        }
    }
}