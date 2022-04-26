using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Validators.Messages
{
    public class CampaignValidator : BaseGrandValidator<CampaignModel>
    {
        public CampaignValidator(
            IEnumerable<IValidatorConsumer<CampaignModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.Campaigns.Fields.Name.Required"));
            RuleFor(x => x.Subject).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.Campaigns.Fields.Subject.Required"));
            RuleFor(x => x.Body).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.Campaigns.Fields.Body.Required"));
        }
    }
}