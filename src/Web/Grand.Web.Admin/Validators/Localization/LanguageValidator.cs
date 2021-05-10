using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Localization;
using System.Collections.Generic;
using System.Globalization;

namespace Grand.Web.Admin.Validators.Localization
{
    public class LanguageValidator : BaseGrandValidator<LanguageModel>
    {
        public LanguageValidator(
            IEnumerable<IValidatorConsumer<LanguageModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.Name.Required"));
            RuleFor(x => x.LanguageCulture)
                .Must(x =>
                          {
                              try
                              {
                                  //create a CultureInfo object
                                  var culture = new CultureInfo(x);
                                  return true;
                              }
                              catch
                              {
                                  return false;
                              }
                          })
                .WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.LanguageCulture.Validation"));

            RuleFor(x => x.UniqueSeoCode).NotEmpty().WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.UniqueSeoCode.Required"));
            RuleFor(x => x.UniqueSeoCode).Length(2).WithMessage(translationService.GetResource("Admin.Configuration.Languages.Fields.UniqueSeoCode.Length"));

        }
    }
}