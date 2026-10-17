using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Messages;

public class CampaignValidator : BaseGrandValidator<CampaignModel>
{
    public CampaignValidator(
        IEnumerable<IValidatorConsumer<CampaignModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("admin.marketing.Campaigns.Fields.Name.Required"));
        RuleFor(x => x.Subject).NotEmpty()
            .WithMessage(translationService.GetResource("admin.marketing.Campaigns.Fields.Subject.Required"));
        RuleFor(x => x.Body).NotEmpty()
            .WithMessage(translationService.GetResource("admin.marketing.Campaigns.Fields.Body.Required"));
    }
}