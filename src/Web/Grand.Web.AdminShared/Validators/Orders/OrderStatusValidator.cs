using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Orders;

public class OrderStatusValidator : BaseGrandValidator<OrderStatusModel>
{
    public OrderStatusValidator(
        IEnumerable<IValidatorConsumer<OrderStatusModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Orders.OrderStatus.Fields.Name.Required"));
    }
}