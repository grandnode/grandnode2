using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Tax;
using Grand.Web.Commands.Models.Customers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Customers
{
    public class CustomerRegisteredCommandHandler : IRequestHandler<CustomerRegisteredCommand, bool>
    {
        private readonly IUserFieldService _userFieldService;
        private readonly IVatService _checkVatService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly ICustomerService _customerService;
        private readonly ICountryService _countryService;
        private readonly ICustomerActionEventService _customerActionEventService;

        private readonly TaxSettings _taxSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly AddressSettings _addressSettings;
        private readonly LanguageSettings _languageSettings;

        public CustomerRegisteredCommandHandler(
            IUserFieldService userFieldService,
            IVatService checkVatService,
            IMessageProviderService messageProviderService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            IAddressAttributeService addressAttributeService,
            ICountryService countryService,
            ICustomerService customerService,
            ICustomerActionEventService customerActionEventService,
            TaxSettings taxSettings,
            CustomerSettings customerSettings,
            AddressSettings addressSettings,
            LanguageSettings languageSettings)
        {
            _userFieldService = userFieldService;
            _checkVatService = checkVatService;
            _messageProviderService = messageProviderService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _addressAttributeService = addressAttributeService;
            _countryService = countryService;
            _customerService = customerService;
            _customerActionEventService = customerActionEventService;
            _taxSettings = taxSettings;
            _customerSettings = customerSettings;
            _addressSettings = addressSettings;
            _languageSettings = languageSettings;
        }

        public async Task<bool> Handle(CustomerRegisteredCommand request, CancellationToken cancellationToken)
        {

            //VAT number
            if (_taxSettings.EuVatEnabled)
            {
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.VatNumber, request.Model.VatNumber);

                var vat = await _checkVatService.GetVatNumberStatus(request.Model.VatNumber);

                await _userFieldService.SaveField(request.Customer,
                    SystemCustomerFieldNames.VatNumberStatusId,
                    (int)vat.status);
            }

            //form fields
            if (_customerSettings.GenderEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Gender, request.Model.Gender);
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.FirstName, request.Model.FirstName);
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.LastName, request.Model.LastName);
            if (_customerSettings.DateOfBirthEnabled)
            {
                DateTime? dateOfBirth = request.Model.ParseDateOfBirth();
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.DateOfBirth, dateOfBirth);
            }
            if (_customerSettings.CompanyEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Company, request.Model.Company);
            if (_customerSettings.StreetAddressEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.StreetAddress, request.Model.StreetAddress);
            if (_customerSettings.StreetAddress2Enabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.StreetAddress2, request.Model.StreetAddress2);
            if (_customerSettings.ZipPostalCodeEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.ZipPostalCode, request.Model.ZipPostalCode);
            if (_customerSettings.CityEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.City, request.Model.City);
            if (_customerSettings.CountryEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.CountryId, request.Model.CountryId);
            if (_customerSettings.CountryEnabled && _customerSettings.StateProvinceEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.StateProvinceId, request.Model.StateProvinceId);
            if (_customerSettings.PhoneEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Phone, request.Model.Phone);
            if (_customerSettings.FaxEnabled)
                await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.Fax, request.Model.Fax);

            //newsletter
            if (_customerSettings.NewsletterEnabled)
            {
                var categories = new List<string>();
                foreach (string formKey in request.Form.Keys)
                {
                    if (formKey.Contains("customernewsletterCategory_"))
                    {
                        try
                        {
                            var category = formKey.Split('_')[1];
                            categories.Add(category);
                        }
                        catch { }
                    }
                }

                //save newsletter value
                var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(request.Model.Email, request.Store.Id);
                if (newsletter != null)
                {
                    newsletter.Categories.Clear();
                    categories.ForEach(x => newsletter.Categories.Add(x));
                    if (request.Model.Newsletter)
                    {
                        newsletter.Active = true;
                        await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(newsletter);
                    }
                }
                else
                {
                    if (request.Model.Newsletter)
                    {
                        var newsLetterSubscription = new NewsLetterSubscription
                        {
                            NewsLetterSubscriptionGuid = Guid.NewGuid(),
                            Email = request.Model.Email,
                            CustomerId = request.Customer.Id,
                            Active = true,
                            StoreId = request.Store.Id,
                            CreatedOnUtc = DateTime.UtcNow
                        };
                        categories.ForEach(x => newsLetterSubscription.Categories.Add(x));
                        await _newsLetterSubscriptionService.InsertNewsLetterSubscription(newsLetterSubscription);
                    }
                }
            }

            //save customer attributes
            await _customerService.UpdateCustomerField(request.Customer, x => x.Attributes, request.CustomerAttributes);

            //insert default address (if possible)
            var defaultAddress = new Address
            {
                FirstName = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
                LastName = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName),
                Email = request.Customer.Email,
                Company = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company),
                VatNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber),
                CountryId = !string.IsNullOrEmpty(request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId)) ?
                            request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId) : "",
                StateProvinceId = !string.IsNullOrEmpty(request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId)) ?
                    request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId) : "",
                City = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City),
                Address1 = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress),
                Address2 = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2),
                ZipPostalCode = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode),
                PhoneNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone),
                FaxNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax),
                CreatedOnUtc = request.Customer.CreatedOnUtc,
            };

            if (await IsAddressValid(defaultAddress))
            {
                //set default address
                request.Customer.Addresses.Add(defaultAddress);
                await _customerService.InsertAddress(defaultAddress, request.Customer.Id);
                request.Customer.BillingAddress = defaultAddress;
                await _customerService.UpdateBillingAddress(defaultAddress, request.Customer.Id);
                request.Customer.ShippingAddress = defaultAddress;
                await _customerService.UpdateShippingAddress(defaultAddress, request.Customer.Id);
            }

            //notifications
            if (_customerSettings.NotifyNewCustomerRegistration)
                await _messageProviderService.SendCustomerRegisteredMessage(request.Customer, request.Store, _languageSettings.DefaultAdminLanguageId);

            //New customer has a free shipping for the first order
            if (_customerSettings.RegistrationFreeShipping)
                await _customerService.UpdateCustomerField(request.Customer, x => x.FreeShipping, true);

            await _customerActionEventService.Registration(request.Customer);

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether address is valid (can be saved)
        /// </summary>
        /// <param name="address">Address to validate</param>
        /// <returns>Result</returns>
        private async Task<bool> IsAddressValid(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            if (String.IsNullOrWhiteSpace(address.FirstName))
                return false;

            if (String.IsNullOrWhiteSpace(address.LastName))
                return false;

            if (String.IsNullOrWhiteSpace(address.Email))
                return false;

            if (_addressSettings.CompanyEnabled &&
                _addressSettings.CompanyRequired &&
                String.IsNullOrWhiteSpace(address.Company))
                return false;

            if (_addressSettings.VatNumberEnabled &&
                _addressSettings.VatNumberRequired &&
                String.IsNullOrWhiteSpace(address.VatNumber))
                return false;

            if (_addressSettings.StreetAddressEnabled &&
                _addressSettings.StreetAddressRequired &&
                String.IsNullOrWhiteSpace(address.Address1))
                return false;

            if (_addressSettings.StreetAddress2Enabled &&
                _addressSettings.StreetAddress2Required &&
                String.IsNullOrWhiteSpace(address.Address2))
                return false;

            if (_addressSettings.ZipPostalCodeEnabled &&
                _addressSettings.ZipPostalCodeRequired &&
                String.IsNullOrWhiteSpace(address.ZipPostalCode))
                return false;


            if (_addressSettings.CountryEnabled)
            {
                if (String.IsNullOrEmpty(address.CountryId))
                    return false;

                var country = await _countryService.GetCountryById(address.CountryId);
                if (country == null)
                    return false;

                if (_addressSettings.StateProvinceEnabled)
                {
                    var states = country.StateProvinces;
                    if (states.Any())
                    {
                        if (String.IsNullOrEmpty(address.StateProvinceId))
                            return false;

                        var state = states.FirstOrDefault(x => x.Id == address.StateProvinceId);
                        if (state == null)
                            return false;
                    }
                }
            }

            if (_addressSettings.CityEnabled &&
                _addressSettings.CityRequired &&
                String.IsNullOrWhiteSpace(address.City))
                return false;

            if (_addressSettings.PhoneEnabled &&
                _addressSettings.PhoneRequired &&
                String.IsNullOrWhiteSpace(address.PhoneNumber))
                return false;

            if (_addressSettings.FaxEnabled &&
                _addressSettings.FaxRequired &&
                String.IsNullOrWhiteSpace(address.FaxNumber))
                return false;

            var attributes = await _addressAttributeService.GetAllAddressAttributes();
            if (attributes.Any(x => x.IsRequired))
                return false;

            return true;
        }

    }
}
