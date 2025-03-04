using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Localization;
using System.Globalization;

namespace Grand.Web.Admin.Validators.Localization;

public class LanguageValidator : BaseGrandValidator<LanguageModel>
{
    public LanguageValidator(
        IEnumerable<IValidatorConsumer<LanguageModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.Name.Required"));
        RuleFor(x => x.LanguageCulture)
            .Must(x =>
            {
                try
                {
                    //create a CultureInfo object
                    var culture = new CultureInfo(x);
                    return culture != null;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage(
                translationService.GetResource("Admin.Configuration.Languages.Fields.LanguageCulture.Validation"));

        RuleFor(x => x.UniqueSeoCode).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.UniqueSeoCode.Required"));
        RuleFor(x => x.UniqueSeoCode).Length(2)
            .WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.UniqueSeoCode.Length"));
    }
}