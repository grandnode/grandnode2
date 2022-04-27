using FluentValidation;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Validators.Settings
{
    public class OrderSettingsValidator : BaseGrandValidator<SalesSettingsModel.OrderSettingsModel>
    {
        public OrderSettingsValidator(
            IEnumerable<IValidatorConsumer<SalesSettingsModel.OrderSettingsModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.GiftVouchers_Activated_OrderStatusId).NotEqual((int)OrderStatusSystem.Pending)
                .WithMessage(translationService.GetResource("Admin.Settings.LoyaltyPoints.PointsForPurchases_Awarded.Pending"));
           
        }
    }
}