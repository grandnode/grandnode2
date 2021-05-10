using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Orders;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Orders
{
    public class CheckoutAttributeValidator : BaseGrandValidator<CheckoutAttributeModel>
    {
        public CheckoutAttributeValidator(
            IEnumerable<IValidatorConsumer<CheckoutAttributeModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Orders.CheckoutAttributes.Fields.Name.Required"));
        }
    }
}