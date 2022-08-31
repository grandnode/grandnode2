using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Affiliates;

namespace Grand.Web.Admin.Validators.Blogs
{
    public class AffiliateValidator : BaseGrandValidator<AffiliateModel>
    {
        public AffiliateValidator(IEnumerable<IValidatorConsumer<AffiliateModel>> validators, 
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Affiliates.Fields.Name.Required"));
        }
    }
}