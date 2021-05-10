using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Discounts;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Discounts
{
    public class DiscountValidator : BaseGrandValidator<DiscountModel>
    {
        public DiscountValidator(
            IEnumerable<IValidatorConsumer<DiscountModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.discounts.Fields.Name.Required"));
            RuleFor(x => x).Must((x, context) =>
            {
                if (x.CalculateByPlugin && string.IsNullOrEmpty(x.DiscountPluginName))
                {
                    return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("admin.marketing.discounts.Fields.DiscountPluginName.Required"));
        }
    }
}