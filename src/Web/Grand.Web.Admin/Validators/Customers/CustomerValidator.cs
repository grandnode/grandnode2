using FluentValidation;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerValidator : BaseGrandValidator<CustomerModel>
    {
        public CustomerValidator(
            IEnumerable<IValidatorConsumer<CustomerModel>> validators,
            ITranslationService translationService,
            ICountryService countryService,
            CustomerSettings customerSettings)
            : base(validators)
        {
            //customer email
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Email.Required"));
            
            //form fields
            if (customerSettings.CountryEnabled && customerSettings.CountryRequired)
            {
                RuleFor(x => x.CountryId)
                    .NotEqual("")
                    .WithMessage(translationService.GetResource("Account.Fields.Country.Required"));
            }
            if (customerSettings.CountryEnabled &&
                customerSettings.StateProvinceEnabled &&
                customerSettings.StateProvinceRequired)
            {
                RuleFor(x => x.StateProvinceId).MustAsync(async (x, y, context) =>
                {
                    var countryId = !string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "";
                    var country = await countryService.GetCountryById(countryId);
                    if (country != null && country.StateProvinces.Any())
                    {
                        //if yes, then ensure that state is selected
                        if (string.IsNullOrEmpty(y))
                        {
                            return false;
                        }
                        if (country.StateProvinces.FirstOrDefault(x => x.Id == y) != null)
                            return true;
                    }
                    return false;
                }).WithMessage(translationService.GetResource("Account.Fields.StateProvince.Required"));
            }
            if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
                RuleFor(x => x.Company).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Company.Required"));
            if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled) 
                RuleFor(x => x.StreetAddress).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.StreetAddress.Required"));
            if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
                RuleFor(x => x.StreetAddress2).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.StreetAddress2.Required"));
            if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
                RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.ZipPostalCode.Required"));
            if (customerSettings.CityRequired && customerSettings.CityEnabled)
                RuleFor(x => x.City).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.City.Required"));
            if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
                RuleFor(x => x.Phone).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Phone.Required"));
            if (customerSettings.FaxRequired && customerSettings.FaxEnabled) 
                RuleFor(x => x.Fax).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Fax.Required"));
        }
    }
}