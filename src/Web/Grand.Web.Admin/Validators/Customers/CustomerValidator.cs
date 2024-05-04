using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Models.Customers;
using System.Text.RegularExpressions;

namespace Grand.Web.Admin.Validators.Customers;

public class CustomerValidator : BaseGrandValidator<CustomerModel>
{
    public CustomerValidator(
        IEnumerable<IValidatorConsumer<CustomerModel>> validators,
        ITranslationService translationService,
        ICountryService countryService,
        IWorkContext workContext,
        ICustomerService customerService,
        IGroupService groupService,
        CustomerSettings customerSettings)
        : base(validators)
    {
        CustomerCreateValidator();
        CustomerEditValidator();

        //customer email
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Email.Required"));

        //form fields
        if (customerSettings.CountryEnabled && customerSettings.CountryRequired)
            RuleFor(x => x.CountryId)
                .NotEqual("")
                .WithMessage(translationService.GetResource("Account.Fields.Country.Required"));

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

                    if (country.StateProvinces.FirstOrDefault(x => x.Id == y) != null)
                        return true;
                }

                return false;
            }).WithMessage(translationService.GetResource("Account.Fields.StateProvince.Required"));

        if (customerSettings.CompanyRequired && customerSettings.CompanyEnabled)
            RuleFor(x => x.Company).NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Company.Required"));
        if (customerSettings.StreetAddressRequired && customerSettings.StreetAddressEnabled)
            RuleFor(x => x.StreetAddress).NotEmpty().WithMessage(
                translationService.GetResource("Admin.Customers.Customers.Fields.StreetAddress.Required"));
        if (customerSettings.StreetAddress2Required && customerSettings.StreetAddress2Enabled)
            RuleFor(x => x.StreetAddress2).NotEmpty().WithMessage(
                translationService.GetResource("Admin.Customers.Customers.Fields.StreetAddress2.Required"));
        if (customerSettings.ZipPostalCodeRequired && customerSettings.ZipPostalCodeEnabled)
            RuleFor(x => x.ZipPostalCode).NotEmpty().WithMessage(
                translationService.GetResource("Admin.Customers.Customers.Fields.ZipPostalCode.Required"));
        if (customerSettings.CityRequired && customerSettings.CityEnabled)
            RuleFor(x => x.City).NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.City.Required"));
        if (customerSettings.PhoneRequired && customerSettings.PhoneEnabled)
            RuleFor(x => x.Phone).NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Phone.Required"));
        if (customerSettings.FaxRequired && customerSettings.FaxEnabled)
            RuleFor(x => x.Fax).NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Customers.Customers.Fields.Fax.Required"));

        RuleFor(x => x).Custom((x, context) =>
        {
            if (!string.IsNullOrEmpty(x.Password))
                if (!string.IsNullOrEmpty(customerSettings.PasswordRegularExpression))
                {
                    var passwordRegex = new Regex(customerSettings.PasswordRegularExpression);
                    if (!passwordRegex.Match(x.Password).Success)
                        context.AddFailure(translationService.GetResource("Account.Fields.Password.Validation"));
                }

            if (string.IsNullOrWhiteSpace(x.Username) & customerSettings.UsernamesEnabled)
                context.AddFailure("The username cannot be empty");
        });

        async Task<string> ValidateCustomerGroups(IList<CustomerGroup> customerGroups, CustomerModel customerModel)
        {
            ArgumentNullException.ThrowIfNull(customerGroups);

            //ensure a customer is not added to both 'Guests' and 'Registered' customer groups
            //ensure that a customer is in at least one required role ('Guests' and 'Registered')
            var isInGuestsGroup =
                customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Guests) != null;
            var isInRegisteredGroup =
                customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Registered) != null;
            var isAdminGroup =
                customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Administrators) != null;
            var isVendorGroup =
                customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Vendors) != null;
            var isStaffGroup = customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.Staff) !=
                               null;
            var isSalesGroup =
                customerGroups.FirstOrDefault(cr => cr.SystemName == SystemCustomerGroupNames.SalesManager) != null;

            switch (isInGuestsGroup)
            {
                case true when isInRegisteredGroup:
                    return "The customer cannot be in both 'Guests' and 'Registered' customer groups";
                case false when !isInRegisteredGroup:
                    return "Add the customer to 'Guests' or 'Registered' customer group";
            }

            if (await groupService.IsSalesManager(workContext.CurrentCustomer) &&
                (isInGuestsGroup || customerGroups.Count != 1))
                return "Sales manager can assign role 'Registered' only";

            if (!await groupService.IsAdmin(workContext.CurrentCustomer) && isAdminGroup)
                return "Only administrators can assign role 'Administrators'";

            switch (isAdminGroup)
            {
                case true when !string.IsNullOrEmpty(customerModel.VendorId):
                    return "A customer who is associated with a vendor can't be assigned the 'Administrator' role";
                case true when !string.IsNullOrEmpty(customerModel.StaffStoreId):
                    return "A customer who is associated with a staff can't be assigned the 'Administrator' role";
            }

            switch (isVendorGroup)
            {
                case true when string.IsNullOrEmpty(customerModel.VendorId):
                    return translationService.GetResource(
                        "Admin.Customers.Customers.CannotBeInVendoGroupWithoutVendorAssociated");
                case true when string.IsNullOrEmpty(customerModel.VendorId):
                    return translationService.GetResource(
                        "Admin.Customers.Customers.CannotBeInVendoGroupWithoutVendorAssociated");
                case true when isStaffGroup:
                    return translationService.GetResource("Admin.Customers.Customers.VendorShouldNotbeStaff");
            }

            if (isStaffGroup && string.IsNullOrEmpty(customerModel.StaffStoreId))
                return translationService.GetResource(
                    "Admin.Customers.Customers.CannotBeInStaffGroupWithoutStaffAssociated");

            if (isSalesGroup && string.IsNullOrEmpty(customerModel.SeId))
                return translationService.GetResource(
                    "Admin.Customers.Customers.CannotBeInSalesManagerGroupWithoutSalesEmployeeAssociated");

            //no errors
            return "";
        }

        void CustomerCreateValidator()
        {
            When(x => string.IsNullOrEmpty(x.Id), () =>
            {
                RuleFor(x => x).CustomAsync(async (x, context, y) =>
                {
                    if (!string.IsNullOrWhiteSpace(x.Email))
                    {
                        if (!CommonHelper.IsValidEmail(x.Email))
                            context.AddFailure(
                                translationService.GetResource("Account.EmailUsernameErrors.NewEmailIsNotValid"));

                        if (x.Email.Length > 100)
                            context.AddFailure(
                                translationService.GetResource("Account.EmailUsernameErrors.EmailTooLong"));

                        var customerByEmail = await customerService.GetCustomerByEmail(x.Email);
                        if (customerByEmail != null)
                            context.AddFailure("Email is already registered");
                    }

                    if (!string.IsNullOrWhiteSpace(x.Owner))
                    {
                        var customerOwner = await customerService.GetCustomerByEmail(x.Owner);
                        if (customerOwner == null)
                            context.AddFailure("Owner email is not exists");
                    }

                    if (!string.IsNullOrWhiteSpace(x.Username) & customerSettings.UsernamesEnabled)
                    {
                        var customerByUsername = await customerService.GetCustomerByUsername(x.Username);
                        if (customerByUsername != null)
                            context.AddFailure("Username is already registered");

                        if (x.Username.Length > 100)
                            context.AddFailure("Username is too long");
                    }

                    //validate customer groups
                    var allCustomerGroups = await groupService.GetAllCustomerGroups(showHidden: true);
                    var newCustomerGroups = allCustomerGroups.Where(customerGroup =>
                        x.CustomerGroups != null && x.CustomerGroups.Contains(customerGroup.Id)).ToList();
                    var customerGroupsError = await ValidateCustomerGroups(newCustomerGroups, x);
                    if (!string.IsNullOrEmpty(customerGroupsError)) context.AddFailure(customerGroupsError);
                });
            });
        }

        void CustomerEditValidator()
        {
            When(x => !string.IsNullOrEmpty(x.Id), () =>
            {
                RuleFor(x => x).CustomAsync(async (x, context, y) =>
                {
                    //validate customer groups
                    var allCustomerGroups = await groupService.GetAllCustomerGroups(showHidden: true);
                    var newCustomerGroups = allCustomerGroups.Where(customerGroup =>
                        x.CustomerGroups != null && x.CustomerGroups.Contains(customerGroup.Id)).ToList();

                    var customerGroupsError = await ValidateCustomerGroups(newCustomerGroups, x);
                    if (!string.IsNullOrEmpty(customerGroupsError)) context.AddFailure(customerGroupsError);

                    if (!string.IsNullOrWhiteSpace(x.Owner))
                    {
                        var customerByOwner = await customerService.GetCustomerByEmail(x.Owner);
                        if (customerByOwner == null)
                            context.AddFailure("Owner email is not exists");

                        if (string.Equals(x.Owner, x.Email, StringComparison.CurrentCultureIgnoreCase))
                            context.AddFailure("You can't assign own email");
                    }

                    var customer = await customerService.GetCustomerById(x.Id);
                    if (await groupService.IsSalesManager(workContext.CurrentCustomer) &&
                        customer?.Id == workContext.CurrentCustomer.Id)
                        context.AddFailure("You can't edit own data from admin panel");

                    if (customer != null && customer.Email != x.Email.ToLower())
                    {
                        if (!CommonHelper.IsValidEmail(x.Email))
                            context.AddFailure(
                                translationService.GetResource("Account.EmailUsernameErrors.NewEmailIsNotValid"));

                        if (x.Email.Length > 100)
                            context.AddFailure(
                                translationService.GetResource("Account.EmailUsernameErrors.EmailTooLong"));

                        var customer2 = await customerService.GetCustomerByEmail(x.Email);
                        if (customer2 != null && customer.Id != customer2.Id)
                            context.AddFailure(
                                translationService.GetResource("Account.EmailUsernameErrors.EmailAlreadyExists"));
                    }

                    if (customer != null && !string.IsNullOrWhiteSpace(x.Username) & customerSettings.UsernamesEnabled)
                    {
                        if (x.Username.Length > 100)
                            context.AddFailure("Username is too long");

                        var user2 = await customerService.GetCustomerByUsername(x.Username);
                        if (user2 != null && customer.Id != user2.Id)
                            context.AddFailure("The username is already in use");
                    }
                });
            });
        }
    }
}