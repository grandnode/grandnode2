using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Validators.Directory
{
    public class CountryValidator : BaseGrandValidator<CountryModel>
    {
        public CountryValidator(
            IEnumerable<IValidatorConsumer<CountryModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Configuration.Countries.Fields.Name.Required"));

            RuleFor(x => x.TwoLetterIsoCode)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Configuration.Countries.Fields.TwoLetterIsoCode.Required"));
            RuleFor(x => x.TwoLetterIsoCode)
                .Length(2)
                .WithMessage(translationService.GetResource("Admin.Configuration.Countries.Fields.TwoLetterIsoCode.Length"));

            RuleFor(x => x.ThreeLetterIsoCode)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Configuration.Countries.Fields.ThreeLetterIsoCode.Required"));
            RuleFor(x => x.ThreeLetterIsoCode)
                .Length(3)
                .WithMessage(translationService.GetResource("Admin.Configuration.Countries.Fields.ThreeLetterIsoCode.Length"));
        }
    }
}