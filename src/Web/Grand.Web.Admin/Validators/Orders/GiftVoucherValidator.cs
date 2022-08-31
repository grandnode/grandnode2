﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Validators.Orders
{
    public class GiftVoucherValidator : BaseGrandValidator<GiftVoucherModel>
    {
        public GiftVoucherValidator(
            IEnumerable<IValidatorConsumer<GiftVoucherModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage(translationService.GetResource("Admin.GiftVouchers.Fields.Code.Required"));
        }
    }
}