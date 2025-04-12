using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Orders;

namespace Grand.Web.AdminShared.Validators.Orders;

public class OrderStatusValidator : BaseGrandValidator<OrderStatusModel>
{
    public OrderStatusValidator(
        IEnumerable<IValidatorConsumer<OrderStatusModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Orders.OrderStatus.Fields.Name.Required"));
    }
}