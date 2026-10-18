using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Directory;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Directory;

public class RobotsTxtValidator : BaseGrandValidator<RobotsTxtModel>
{
    public RobotsTxtValidator(
        IEnumerable<IValidatorConsumer<RobotsTxtModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.RobotsTxt.Fields.Name.Required"));

        //no rule on Text: an empty robots.txt is valid (it allows everything) and is what a store
        //starts with, so it must stay possible to save one
    }
}