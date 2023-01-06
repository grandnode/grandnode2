﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Extensions;
using Grand.Web.Models.Customer;

namespace Grand.Web.Validators.Customer
{
    public class CustomerInfoValidator : BaseGrandValidator<CustomerInfoModel>
    {
        public CustomerInfoValidator(
            IEnumerable<IValidatorConsumer<CustomerInfoModel>> validators,
            ITranslationService translationService,
            ICountryService countryService,
            CustomerSettings customerSettings)
            : base(validators)
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Email.Required"));
            RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

            if (customerSettings.FirstLastNameRequired)
            {
                RuleFor(x => x.FirstName).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.FirstName.Required"));
                RuleFor(x => x.LastName).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.LastName.Required"));
            }

            if (customerSettings.UsernamesEnabled && customerSettings.AllowUsersToChangeUsernames)
            {
                RuleFor(x => x.Username).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Username.Required"));
            }

            //form fields
            if (customerSettings.CountryEnabled && customerSettings.CountryRequired)
            {
                RuleFor(x => x.CountryId)
                    .NotNull()
                    .WithMessage(translationService.GetResource("Address.Fields.Country.Required"));
                RuleFor(x => x.CountryId)
                    .NotEqual("")
                    .WithMessage(translationService.GetResource("Address.Fields.Country.Required"));
            }
            if (customerSettings.CountryEnabled &&
                customerSettings.StateProvinceEnabled &&
                customerSettings.StateProvinceRequired)
            {
                RuleFor(x => x.StateProvinceId).MustAsync(async (x, y, _) =>
                {
                    var countryId = !string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "";
                    var country = await countryService.GetCountryById(countryId);
                    if (country == null || !country.StateProvinces.Any()) return false;
                    //if yes, then ensure that state is selected
                    if (string.IsNullOrEmpty(y))
                    {
                        return false;
                    }
                    return country.StateProvinces.FirstOrDefault(s => s.Id == y) != null;
                }).WithMessage(translationService.GetResource("Account.Fields.StateProvince.Required"));
            }
            if (customerSettings.DateOfBirthEnabled && customerSettings.DateOfBirthRequired)
            {
                RuleFor(x => x.DateOfBirthDay).Must((x, _) =>
                {
                    var dateOfBirth = x.ParseDateOfBirth();
                    return dateOfBirth.HasValue;
                }).WithMessage(translationService.GetResource("Account.Fields.DateOfBirth.Required"));

                //minimum age
                RuleFor(x => x.DateOfBirthDay).Must((x, _) =>
                {
                    var dateOfBirth = x.ParseDateOfBirth();
                    return !dateOfBirth.HasValue || !customerSettings.DateOfBirthMinimumAge.HasValue ||
                           CommonHelper.GetDifferenceInYears(dateOfBirth.Value, DateTime.Today) >=
                           customerSettings.DateOfBirthMinimumAge.Value;
                }).WithMessage(string.Format(translationService.GetResource("Account.Fields.DateOfBirth.MinimumAge"), customerSettings.DateOfBirthMinimumAge));
            }
            if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            {
                RuleFor(x => x.Company).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Company.Required"));
            }
            if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            {
                RuleFor(x => x.StreetAddress).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.StreetAddress.Required"));
            }
            if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            {
                RuleFor(x => x.StreetAddress2).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.StreetAddress2.Required"));
            }
            if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            {
                RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.ZipPostalCode.Required"));
            }
            if (customerSettings.CityRequired && customerSettings.CityEnabled)
            {
                RuleFor(x => x.City).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.City.Required"));
            }
            if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            {
                RuleFor(x => x.Phone).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Phone.Required"));
            }
            if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            {
                RuleFor(x => x.Fax).NotEmpty().WithMessage(translationService.GetResource("Account.Fields.Fax.Required"));
            }
        }
    }
}