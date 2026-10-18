using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Localization;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Localization;

public class LanguageResourceValidator : BaseGrandValidator<LanguageResourceModel>
{
    public LanguageResourceValidator(
        IEnumerable<IValidatorConsumer<LanguageResourceModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Languages.Resources.Fields.Name.Required"));
        RuleFor(x => x.Value).NotEmpty()
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Languages.Resources.Fields.Value.Required"));
    }
}