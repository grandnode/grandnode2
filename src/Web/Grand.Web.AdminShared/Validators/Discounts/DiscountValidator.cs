using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Discounts;

public class DiscountValidator : BaseGrandValidator<DiscountModel>
{
    public DiscountValidator(
        IEnumerable<IValidatorConsumer<DiscountModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("admin.marketing.discounts.Fields.Name.Required"));
        RuleFor(x => x).Must((x, _) =>
        {
            if (x.CalculateByPlugin && string.IsNullOrEmpty(x.DiscountPluginName)) return false;
            return true;
        }).WithMessage(translationService.GetResource("admin.marketing.discounts.Fields.DiscountPluginName.Required"));
    }
}