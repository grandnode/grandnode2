using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Affiliates;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Affiliate;

public class AffiliateValidator : BaseGrandValidator<AffiliateModel>
{
    public AffiliateValidator(IEnumerable<IValidatorConsumer<AffiliateModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Affiliates.Fields.Name.Required"));
    }
}