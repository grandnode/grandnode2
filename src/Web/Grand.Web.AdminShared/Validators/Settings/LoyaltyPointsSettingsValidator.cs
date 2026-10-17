using FluentValidation;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Settings;

public class LoyaltyPointsSettingsValidator : BaseGrandValidator<SalesSettingsModel.LoyaltyPointsSettingsModel>
{
    public LoyaltyPointsSettingsValidator(
        IEnumerable<IValidatorConsumer<SalesSettingsModel.LoyaltyPointsSettingsModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.PointsForPurchases_Awarded).NotEqual((int)OrderStatusSystem.Pending).WithMessage(
            translationService.GetResource("Admin.Settings.LoyaltyPoints.PointsForPurchases_Awarded.Pending"));
    }
}