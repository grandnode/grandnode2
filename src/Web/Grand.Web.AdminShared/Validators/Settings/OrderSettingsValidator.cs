using FluentValidation;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Settings;

public class OrderSettingsValidator : BaseGrandValidator<SalesSettingsModel.OrderSettingsModel>
{
    public OrderSettingsValidator(
        IEnumerable<IValidatorConsumer<SalesSettingsModel.OrderSettingsModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.GiftVouchers_Activated_OrderStatusId).NotEqual((int)OrderStatusSystem.Pending)
            .WithMessage(
                translationService.GetResource("Admin.Settings.LoyaltyPoints.PointsForPurchases_Awarded.Pending"));
    }
}