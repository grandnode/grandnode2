using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Extensions;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Validators.Customer;

public class CustomerInfoValidator : BaseGrandValidator<CustomerInfoModel>
{
    public CustomerInfoValidator(
        IEnumerable<IValidatorConsumer<CustomerInfoModel>> validators,
        IContextAccessor contextAccessor,
        ICustomerService customerService,
        IMediator mediator, ICustomerAttributeParser customerAttributeParser,
        ITranslationService translationService,
        ICountryService countryService,
        CustomerSettings customerSettings,
        CustomerConfig customerConfig)
        : base(validators)
    {
        RuleFor(x => x.Email).NotEmpty()
            .WithMessage(translationService.GetResource("Account.Fields.Email.Required"));
        RuleFor(x => x.Email).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        if (customerSettings.FirstLastNameRequired)
        {
            RuleFor(x => x.FirstName).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.FirstName.Required"));
            RuleFor(x => x.LastName).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.LastName.Required"));
        }

        if (customerSettings.UsernamesEnabled && customerSettings.AllowUsersToChangeUsernames)
            RuleFor(x => x.Username).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.Username.Required"));

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
            RuleFor(x => x.StateProvinceId).MustAsync(async (x, y, _) =>
            {
                var countryId = !string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "";
                var country = await countryService.GetCountryById(countryId);
                if (country == null || !country.StateProvinces.Any()) return false;
                //if yes, then ensure that state is selected
                if (string.IsNullOrEmpty(y)) return false;

                return country.StateProvinces.FirstOrDefault(s => s.Id == y) != null;
            }).WithMessage(translationService.GetResource("Account.Fields.StateProvince.Required"));

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
            }).WithMessage(string.Format(translationService.GetResource("Account.Fields.DateOfBirth.MinimumAge"),
                customerSettings.DateOfBirthMinimumAge));
        }

        if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            RuleFor(x => x.Company).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.Company.Required"));

        if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            RuleFor(x => x.StreetAddress).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.StreetAddress.Required"));

        if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            RuleFor(x => x.StreetAddress2).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.StreetAddress2.Required"));

        if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            RuleFor(x => x.ZipPostalCode).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.ZipPostalCode.Required"));

        if (customerSettings.CityRequired && customerSettings.CityEnabled)
            RuleFor(x => x.City).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.City.Required"));

        if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            RuleFor(x => x.Phone).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.Phone.Required"));

        if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            RuleFor(x => x.Fax).NotEmpty()
                .WithMessage(translationService.GetResource("Account.Fields.Fax.Required"));

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var customerAttributes = await mediator.Send(new GetParseCustomAttributes
                { SelectedAttributes = x.SelectedAttributes }, _);
            var customerAttributeWarnings = await customerAttributeParser.GetAttributeWarnings(customerAttributes,
                contextAccessor.StoreContext.CurrentStore.Id);
            foreach (var error in customerAttributeWarnings) context.AddFailure(error);

            //the same values the command stores, checked where the account lives
            var currentCustomer = contextAccessor.WorkContext.CurrentCustomer;
            var storeId = customerConfig.RegisterCustomersPerStore ? currentCustomer.StoreId : "";

            var email = x.Email?.Trim().ToLowerInvariant();
            var sameAddress = string.Equals(currentCustomer.Email, email, StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(email) && currentCustomer.Email != email &&
                (sameAddress || customerSettings.AllowUsersToChangeEmail))
            {
                if (!CommonHelper.IsValidEmail(email))
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.NewEmailIsNotValid"));

                if (email.Length > 100)
                    context.AddFailure(translationService.GetResource("Account.EmailUsernameErrors.EmailTooLong"));

                var customer2 = await customerService.GetCustomerByEmail(email, storeId);
                if (customer2 != null && customer2.Id != currentCustomer.Id)
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.EmailAlreadyExists"));
            }

            var username = x.Username?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(username) && customerSettings.UsernamesEnabled &&
                customerSettings.AllowUsersToChangeUsernames && currentCustomer.Username != username)
            {
                if (username.Length > 100)
                    context.AddFailure(translationService.GetResource("Account.EmailUsernameErrors.UsernameTooLong"));

                var customer2 = await customerService.GetCustomerByUsername(username, storeId);
                if (customer2 != null && customer2.Id != currentCustomer.Id)
                    context.AddFailure(
                        translationService.GetResource("Account.EmailUsernameErrors.UsernameAlreadyExists"));
            }
        });
    }
}