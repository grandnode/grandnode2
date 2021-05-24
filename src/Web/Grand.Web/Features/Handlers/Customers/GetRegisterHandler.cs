using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Web.Common.Security.Captcha;
using Grand.Domain.Customers;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Web.Models.Newsletter;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetRegisterHandler : IRequestHandler<GetRegister, RegisterModel>
    {

        private readonly INewsletterCategoryService _newsletterCategoryService;
        private readonly ITranslationService _translationService;
        private readonly ICountryService _countryService;
        private readonly IMediator _mediator;

        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;
        private readonly CaptchaSettings _captchaSettings;

        public GetRegisterHandler(
            INewsletterCategoryService newsletterCategoryService,
            ITranslationService translationService,
            ICountryService countryService,
            IMediator mediator,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            CaptchaSettings captchaSettings)
        {
            _newsletterCategoryService = newsletterCategoryService;
            _translationService = translationService;
            _countryService = countryService;
            _mediator = mediator;
            _customerSettings = customerSettings;
            _taxSettings = taxSettings;
            _captchaSettings = captchaSettings;
        }

        public async Task<RegisterModel> Handle(GetRegister request, CancellationToken cancellationToken)
        {
            var model = new RegisterModel();
            if (request.Model != null)
                model = request.Model;
            else
                //enable newsletter by default
                model.Newsletter = _customerSettings.NewsletterTickedByDefault;

            model.DisplayVatNumber = _taxSettings.EuVatEnabled;
            //form fields
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
            model.AcceptPrivacyPolicyEnabled = _customerSettings.AcceptPrivacyPolicyEnabled;
            model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
            model.CheckUsernameAvailabilityEnabled = _customerSettings.CheckUsernameAvailabilityEnabled;
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnRegistrationPage;

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

            //custom customer attributes
            var customAttributes = await _mediator.Send(new GetCustomAttributes() {
                Customer = request.Customer,
                Language = request.Language,
                OverrideAttributes = request.OverrideCustomCustomerAttributes
            });
            foreach (var item in customAttributes.Where(x=>!x.IsReadOnly))
            {
                model.CustomerAttributes.Add(item);
            }

            //newsletter categories
            var newsletterCategories = await _newsletterCategoryService.GetNewsletterCategoriesByStore(request.Store.Id);
            foreach (var item in newsletterCategories)
            {
                model.NewsletterCategories.Add(new NewsletterSimpleCategory() {
                    Id = item.Id,
                    Name = item.GetTranslation(x => x.Name, request.Language.Id),
                    Description = item.GetTranslation(x => x.Description, request.Language.Id),
                    Selected = item.Selected
                });
            }
            return model;
        }
    }
}
