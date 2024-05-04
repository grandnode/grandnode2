using FluentValidation;
using Grand.Api.DTOs.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Customers;

public class CustomerValidator : BaseGrandValidator<CustomerDto>
{
    public CustomerValidator(
        IEnumerable<IValidatorConsumer<CustomerDto>> validators,
        ITranslationService translationService, ICountryService countryService,
        ICustomerService customerService, IStoreService storeService, CustomerSettings customerSettings)
        : base(validators)
    {
        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            var customer = await customerService.GetCustomerByEmail(x.Email);
            return customer == null || customer.Id == x.Id;
        }).WithMessage(translationService.GetResource("Api.Customers.Customer.Fields.Email.Registered"));

        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            var username = await customerService.GetCustomerByUsername(x.Username);
            return username == null || username.Id == x.Id || !customerSettings.UsernamesEnabled;
        }).WithMessage(translationService.GetResource("Api.Customers.Customer.Fields.Username.Registered"));

        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            var customer = await customerService.GetCustomerByGuid(x.CustomerGuid);
            return customer == null || customer.Id == x.Id;
        }).WithMessage(translationService.GetResource("Api.Customers.Customer.Fields.Guid.Exists"));

        RuleFor(x => x).MustAsync(async (x, _) =>
        {
            var store = await storeService.GetStoreById(x.StoreId);
            return store != null;
        }).WithMessage(translationService.GetResource("Api.Customers.Customer.Fields.StoreId.NotExists"));


        //form fields
        if (customerSettings.CountryEnabled && customerSettings.CountryRequired)
            RuleFor(x => x.CountryId)
                .NotEqual("")
                .WithMessage(translationService.GetResource("Api.Customers.Customer.Fields.Country.Required"));
        if (customerSettings.CountryEnabled &&
            customerSettings.StateProvinceEnabled &&
            customerSettings.StateProvinceRequired)
            RuleFor(x => x.StateProvinceId).MustAsync(async (x, y, _) =>
            {
                var countryId = !string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "";
                var country = await countryService.GetCountryById(countryId);
                if (country != null && country.StateProvinces.Any())
                {
                    //if yes, then ensure that state is selected
                    if (string.IsNullOrEmpty(y)) return false;
                    if (country.StateProvinces.FirstOrDefault(stateProvince => stateProvince.Id == y) != null)
                        return true;
                }

                return false;
            }).WithMessage(translationService.GetResource("pi.Customers.Customer.Fields.StateProvince.Required"));
        if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            RuleFor(x => x.Company).NotEmpty()
                .WithMessage(
                    translationService.GetResource(
                        "Api.Customers.Customer.Customers.Customers.Fields.Company.Required"));
        if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            RuleFor(x => x.StreetAddress).NotEmpty().WithMessage(
                translationService.GetResource(
                    "Api.Customers.Customer.Customers.Customers.Fields.StreetAddress.Required"));
        if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            RuleFor(x => x.StreetAddress2).NotEmpty().WithMessage(
                translationService.GetResource(
                    "Api.Customers.Customer.Customers.Customers.Fields.StreetAddress2.Required"));
        if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(
                translationService.GetResource(
                    "Api.Customers.Customer.Customers.Customers.Fields.ZipPostalCode.Required"));
        if (customerSettings.CityRequired && customerSettings.CityEnabled)
            RuleFor(x => x.City).NotEmpty()
                .WithMessage(
                    translationService.GetResource("Api.Customers.Customer.Customers.Customers.Fields.City.Required"));
        if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            RuleFor(x => x.Phone).NotEmpty()
                .WithMessage(
                    translationService.GetResource("Api.Customers.Customer.Customers.Customers.Fields.Phone.Required"));
        if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            RuleFor(x => x.Fax).NotEmpty()
                .WithMessage(
                    translationService.GetResource("Api.Customers.Customer.Customers.Customers.Fields.Fax.Required"));
    }
}