using FluentValidation;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Orders;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Orders
{
    public class OrderStatusValidator : BaseGrandValidator<OrderStatusModel>
    {
        public OrderStatusValidator(
            IEnumerable<IValidatorConsumer<OrderStatusModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Orders.OrderStatus.Fields.Name.Required"));
        }
    }
}