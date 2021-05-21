using FluentValidation;
using Grand.Api.DTOs.Customers;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Common;
using Grand.Infrastructure.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Api.Validators.Customers
{
    public class AddressValidator : BaseGrandValidator<AddressDto>
    {
        public AddressValidator(
            IEnumerable<IValidatorConsumer<AddressDto>> validators,
            ITranslationService translationService, ICountryService countryService, AddressSettings addressSettings)
            : base(validators)
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Api.Customers.Address.Fields.FirstName.Required"));
            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Api.Customers.Address.Fields.LastName.Required"));
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Api.Customers.Address.Fields.Email.Required"));
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage(translationService.GetResource("Api.Customers.Address.Common.WrongEmail"));
            if (addressSettings.CountryEnabled)
            {
                RuleFor(x => x.CountryId)
                    .NotNull()
                    .WithMessage(translationService.GetResource("Api.Customers.Address.Fields.Country.Required"));
                RuleFor(x => x.CountryId)
                    .NotEqual("")
                    .WithMessage(translationService.GetResource("Api.Customers.Address.Fields.Country.Required"));
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
                }).WithMessage(translationService.GetResource("Api.Customers.Address.Fields.StateProvince.Required"));
            }
            if (addressSettings.CompanyRequired && addressSettings.CompanyEnabled)
            {
                RuleFor(x => x.Company).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.Company.Required"));
            }
            if (addressSettings.VatNumberRequired && addressSettings.VatNumberEnabled)
            {
                RuleFor(x => x.VatNumber).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.VatNumber.Required"));
            }
            if (addressSettings.StreetAddressRequired && addressSettings.StreetAddressEnabled)
            {
                RuleFor(x => x.Address1).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.StreetAddress.Required"));
            }
            if (addressSettings.StreetAddress2Required && addressSettings.StreetAddress2Enabled)
            {
                RuleFor(x => x.Address2).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.StreetAddress2.Required"));
            }
            if (addressSettings.ZipPostalCodeRequired && addressSettings.ZipPostalCodeEnabled)
            {
                RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.ZipPostalCode.Required"));
            }
            if (addressSettings.CityRequired && addressSettings.CityEnabled)
            {
                RuleFor(x => x.City).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.City.Required"));
            }
            if (addressSettings.PhoneRequired && addressSettings.PhoneEnabled)
            {
                RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.Phone.Required"));
            }
            if (addressSettings.FaxRequired && addressSettings.FaxEnabled)
            {
                RuleFor(x => x.FaxNumber).NotEmpty().WithMessage(translationService.GetResource("Api.Customers.Address.Fields.Fax.Required"));
            }
        }
    }

}
