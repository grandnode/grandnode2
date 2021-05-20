using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Web.Models.Newsletter;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetInfoHandler : IRequestHandler<GetInfo, CustomerInfoModel>
    {
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly INewsletterCategoryService _newsletterCategoryService;
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly IMediator _mediator;
        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;

        public GetInfoHandler(
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            INewsletterCategoryService newsletterCategoryService,
            ITranslationService translationService,
            ICountryService countryService,
            IExternalAuthenticationService externalAuthenticationService,
            IMediator mediator,
            CustomerSettings customerSettings,
            TaxSettings taxSettings)
        {
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _newsletterCategoryService = newsletterCategoryService;
            _translationService = translationService;
            _countryService = countryService;
            _externalAuthenticationService = externalAuthenticationService;
            _mediator = mediator;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
        }

        public async Task<CustomerInfoModel> Handle(GetInfo request, CancellationToken cancellationToken)
        {
            var model = new CustomerInfoModel();
            if (request.Model != null)
                model = request.Model;

            if (!request.ExcludeProperties)
            {
                PrepareNotExludeModel(model, request);
            }
            else
            {
                if (_customerSettings.UsernamesEnabled && !_customerSettings.AllowUsersToChangeUsernames)
                    model.Username = request.Customer.Username;
            }

            //newsletter
            await PrepareNewsletter(model, request);

            //settings
            await PrepareModelSettings(model, request);

            //external authentication
            await PrepareExternalAuth(model, request);

            //custom customer attributes
            var customAttributes = await _mediator.Send(new GetCustomAttributes() {
                Customer = request.Customer,
                Language = request.Language,
                OverrideAttributes = request.OverrideCustomCustomerAttributes
            });
            foreach (var attribute in customAttributes)
                model.CustomerAttributes.Add(attribute);

            return model;
        }

        private void PrepareNotExludeModel(CustomerInfoModel model, GetInfo request)
        {
            model.VatNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber);
            model.FirstName = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);
            model.LastName = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);
            model.Gender = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender);
            var dateOfBirth = request.Customer.GetUserFieldFromEntity<DateTime?>(SystemCustomerFieldNames.DateOfBirth);
            if (dateOfBirth.HasValue)
            {
                model.DateOfBirthDay = dateOfBirth.Value.Day;
                model.DateOfBirthMonth = dateOfBirth.Value.Month;
                model.DateOfBirthYear = dateOfBirth.Value.Year;
            }
            model.Company = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company);
            model.StreetAddress = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress);
            model.StreetAddress2 = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2);
            model.ZipPostalCode = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode);
            model.City = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City);
            model.CountryId = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId);
            model.StateProvinceId = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId);
            model.Phone = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);
            model.Fax = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax);
            model.Email = request.Customer.Email;
            model.Username = request.Customer.Username;
        }

        private async Task PrepareNewsletter(CustomerInfoModel model, GetInfo request)
        {
            //newsletter
            var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(request.Customer.Email, request.Store.Id);
            //TODO - it is necessary ?
            //if (newsletter == null)
            //    newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByCustomerId(request.Customer.Id);

            model.Newsletter = newsletter != null && newsletter.Active;

            var categories = (await _newsletterCategoryService.GetAllNewsletterCategory()).ToList();
            categories.ForEach(x => model.NewsletterCategories.Add(new NewsletterSimpleCategory() {
                Id = x.Id,
                Description = x.GetTranslation(y => y.Description, request.Language.Id),
                Name = x.GetTranslation(y => y.Name, request.Language.Id),
                Selected = newsletter == null ? false : newsletter.Categories.Contains(x.Id),
            }));
        }

        private async Task PrepareModelSettings(CustomerInfoModel model, GetInfo request)
        {
            //countries and states
            if (_customerSettings.CountryEnabled)
            {
                model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectCountry"), Value = "" });
                foreach (var c in await _countryService.GetAllCountries(request.Language.Id, request.Store.Id))
                {
                    model.AvailableCountries.Add(new SelectListItem {
                        Text = c.GetTranslation(x => x.Name, request.Language.Id),
                        Value = c.Id.ToString(),
                        Selected = c.Id == model.CountryId
                    });
                }

                if (_customerSettings.StateProvinceEnabled)
                {
                    //states
                    var states = await _countryService.GetStateProvincesByCountryId(model.CountryId, request.Language.Id);
                    model.AvailableStates.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectState"), Value = "" });

                    foreach (var s in states)
                    {
                        model.AvailableStates.Add(new SelectListItem { Text = s.GetTranslation(x => x.Name, request.Language.Id), Value = s.Id.ToString(), Selected = (s.Id == model.StateProvinceId) });
                    }
                }
            }

            model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            model.VatNumberStatusNote = ((VatNumberStatus)request.Customer.GetUserFieldFromEntity<int>(SystemCustomerFieldNames.VatNumberStatusId))
                .GetTranslationEnum(_translationService, request.Language.Id);
            model.GenderEnabled = _customerSettings.GenderEnabled;
            model.DateOfBirthEnabled = _customerSettings.DateOfBirthEnabled;
            model.DateOfBirthRequired = _customerSettings.DateOfBirthRequired;
            model.FirstLastNameRequired = _customerSettings.FirstLastNameRequired;
            model.CompanyEnabled = _customerSettings.CompanyEnabled;
            model.CompanyRequired = _customerSettings.CompanyRequired;
            model.StreetAddressEnabled = _customerSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _customerSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _customerSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _customerSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _customerSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _customerSettings.ZipPostalCodeRequired;
            model.CityEnabled = _customerSettings.CityEnabled;
            model.CityRequired = _customerSettings.CityRequired;
            model.CountryEnabled = _customerSettings.CountryEnabled;
            model.CountryRequired = _customerSettings.CountryRequired;
            model.StateProvinceEnabled = _customerSettings.StateProvinceEnabled;
            model.StateProvinceRequired = _customerSettings.StateProvinceRequired;
            model.PhoneEnabled = _customerSettings.PhoneEnabled;
            model.PhoneRequired = _customerSettings.PhoneRequired;
            model.FaxEnabled = _customerSettings.FaxEnabled;
            model.FaxRequired = _customerSettings.FaxRequired;
            model.NewsletterEnabled = _customerSettings.NewsletterEnabled;
            model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
            model.AllowUsersToChangeUsernames = _customerSettings.AllowUsersToChangeUsernames;
            model.AllowUsersToChangeEmail = _customerSettings.AllowUsersToChangeEmail;
            model.CheckUsernameAvailabilityEnabled = _customerSettings.CheckUsernameAvailabilityEnabled;
            model.Is2faEnabled = request.Customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled);

        }

        private async Task PrepareExternalAuth(CustomerInfoModel model, GetInfo request)
        {
            model.NumberOfExternalAuthenticationProviders = _externalAuthenticationService
                          .LoadActiveAuthenticationProviders(request.Customer, request.Store).Count;
            foreach (var ear in await _externalAuthenticationService.GetExternalIdentifiers(request.Customer))
            {
                if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(ear.ProviderSystemName))
                    continue;

                var authMethod = _externalAuthenticationService.LoadAuthenticationProviderBySystemName(ear.ProviderSystemName);

                model.AssociatedExternalAuthRecords.Add(new CustomerInfoModel.AssociatedExternalAuthModel {
                    Id = ear.Id,
                    Email = ear.Email,
                    ExternalIdentifier = ear.ExternalDisplayIdentifier,
                    AuthMethodName = authMethod.FriendlyName
                });
            }
        }
    }
}
