using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class BannerValidator : BaseGrandValidator<BannerModel>
    {
        public BannerValidator(
            IEnumerable<IValidatorConsumer<BannerModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.Banners.Fields.Name.Required"));
            RuleFor(x => x.Body).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.Banners.Fields.Body.Required"));
        }
    }
}