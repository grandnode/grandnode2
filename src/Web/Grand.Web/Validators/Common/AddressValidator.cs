using FluentValidation;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Validators.Common
{
    public class AddressValidator : BaseGrandValidator<AddressModel>
    {
        public AddressValidator(
            IEnumerable<IValidatorConsumer<AddressModel>> validators,
            ITranslationService translationService,
            ICountryService countryService,
            AddressSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Address.Fields.FirstName.Required"));
            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Address.Fields.LastName.Required"));
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Address.Fields.Email.Required"));
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage(translationService.GetResource("Common.WrongEmail"));
            if (addressSettings.CountryEnabled)
            {
                RuleFor(x => x.CountryId)
                    .NotNull()
                    .WithMessage(translationService.GetResource("Address.Fields.Country.Required"));
                RuleFor(x => x.CountryId)
                    .NotEqual("")
                    .WithMessage(translationService.GetResource("Address.Fields.Country.Required"));
            }
            if (addressSettings.CountryEnabled && addressSettings.StateProvinceEnabled)
            {
                RuleFor(x => x.StateProvinceId).MustAsync(async (x, y, context) =>
                {
                    var countryId = !string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "";
                    var country = await countryService.GetCountryById(countryId);
                    if (country != null && country.StateProvinces.Any())
                    {
                        //if yes, then ensure that state is selected
                        if (String.IsNullOrEmpty(y))
                        {
                            return false;
                        }
                        if (country.StateProvinces.FirstOrDefault(x => x.Id == y) != null)
                            return true;
                    }
                    return false;
                }).WithMessage(translationService.GetResource("Address.Fields.StateProvince.Required"));
            }
            if (addressSettings.CompanyRequired && addressSettings.CompanyEnabled)
            {
                RuleFor(x => x.Company).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Company.Required"));
            }
            if (addressSettings.VatNumberRequired && addressSettings.VatNumberEnabled)
            {
                RuleFor(x => x.VatNumber).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.VatNumber.Required"));
            }
            if (addressSettings.StreetAddressRequired && addressSettings.StreetAddressEnabled)
            {
                RuleFor(x => x.Address1).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.StreetAddress.Required"));
            }
            if (addressSettings.StreetAddress2Required && addressSettings.StreetAddress2Enabled)
            {
                RuleFor(x => x.Address2).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.StreetAddress2.Required"));
            }
            if (addressSettings.ZipPostalCodeRequired && addressSettings.ZipPostalCodeEnabled)
            {
                RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.ZipPostalCode.Required"));
            }
            if (addressSettings.CityRequired && addressSettings.CityEnabled)
            {
                RuleFor(x => x.City).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.City.Required"));
            }
            if (addressSettings.PhoneRequired && addressSettings.PhoneEnabled)
            {
                RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Phone.Required"));
            }
            if (addressSettings.FaxRequired && addressSettings.FaxEnabled)
            {
                RuleFor(x => x.FaxNumber).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Fax.Required"));
            }
        }
    }
}