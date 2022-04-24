using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Validators.Directory
{
    public class RobotsTxtValidator : BaseGrandValidator<RobotsTxtModel>
    {
        public RobotsTxtValidator(
            IEnumerable<IValidatorConsumer<RobotsTxtModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Configuration.RobotsTxt.Fields.Name.Required"));

            RuleFor(x => x.Text)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Configuration.RobotsTxt.Fields.Text.Required"));
        }
    }
}