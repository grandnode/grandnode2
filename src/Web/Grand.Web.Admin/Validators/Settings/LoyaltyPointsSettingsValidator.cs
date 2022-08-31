﻿using FluentValidation;
using Grand.Domain.Orders;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Validators.Settings
{
    public class LoyaltyPointsSettingsValidator : BaseGrandValidator<SalesSettingsModel.LoyaltyPointsSettingsModel>
    {
        public LoyaltyPointsSettingsValidator(
            IEnumerable<IValidatorConsumer<SalesSettingsModel.LoyaltyPointsSettingsModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.PointsForPurchases_Awarded).NotEqual((int)OrderStatusSystem.Pending).WithMessage(translationService.GetResource("Admin.Settings.LoyaltyPoints.PointsForPurchases_Awarded.Pending"));
        }
    }
}