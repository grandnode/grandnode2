using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Vendors;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Vendors;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Common
{
    public class GetVendorAddressHandler : IRequestHandler<GetVendorAddress, VendorAddressModel>
    {
        private readonly ICountryService _countryService;
        private readonly ITranslationService _translationService;

        private readonly VendorSettings _vendorSettings;

        public GetVendorAddressHandler(
            ICountryService countryService,
            ITranslationService translationService,
            VendorSettings vendorSettings)
        {
            _countryService = countryService;
            _translationService = translationService;
            _vendorSettings = vendorSettings;
        }

        public async Task<VendorAddressModel> Handle(GetVendorAddress request, CancellationToken cancellationToken)
        {
            var model = request.Model ?? new VendorAddressModel();

            if (!request.ExcludeProperties && request.Address != null)
            {
                model.Company = request.Address.Company;
                model.CountryId = request.Address.CountryId;
                Country country = null;
                if (!String.IsNullOrEmpty(request.Address.CountryId))
                    country = await _countryService.GetCountryById(request.Address.CountryId);
                model.CountryName = country != null ? country.GetTranslation(x => x.Name, request.Language.Id) : null;

                model.StateProvinceId = request.Address.StateProvinceId;
                StateProvince state = null;
                if (!String.IsNullOrEmpty(request.Address.StateProvinceId) && country != null)
                    state = country.StateProvinces.FirstOrDefault(x => x.Id == request.Address.StateProvinceId);
                model.StateProvinceName = state != null ? state.GetTranslation(x => x.Name, request.Language.Id) : null;

                model.City = request.Address.City;
                model.Address1 = request.Address.Address1;
                model.Address2 = request.Address.Address2;
                model.ZipPostalCode = request.Address.ZipPostalCode;
                model.PhoneNumber = request.Address.PhoneNumber;
                model.FaxNumber = request.Address.FaxNumber;
            }

            if (request.Address == null && request.PrePopulateWithCustomerFields)
            {
                if (request.Customer == null)
                    throw new Exception("Customer cannot be null when prepopulating an address");
                model.Company = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company);
                model.Address1 = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress);
                model.Address2 = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2);
                model.ZipPostalCode = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode);
                model.City = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City);
                model.PhoneNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);
                model.FaxNumber = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax);

                if (_vendorSettings.CountryEnabled)
                    model.CountryId = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId);

                if (_vendorSettings.StateProvinceEnabled)
                    model.StateProvinceId = request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId);
            }

            //countries and states
            if (_vendorSettings.CountryEnabled && request.LoadCountries != null)
            {
                model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectCountry"), Value = "" });
                foreach (var c in request.LoadCountries())
                {
                    model.AvailableCountries.Add(new SelectListItem {
                        Text = c.GetTranslation(x => x.Name, request.Language.Id),
                        Value = c.Id.ToString(),
                        Selected = c.Id == model.CountryId
                    });
                }

                if (_vendorSettings.StateProvinceEnabled)
                {
                    var states = await _countryService
                        .GetStateProvincesByCountryId(!String.IsNullOrEmpty(model.CountryId) ? model.CountryId : "", request.Language.Id);
                    
                    model.AvailableStates.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectState"), Value = "" });
                    foreach (var s in states)
                    {
                        model.AvailableStates.Add(new SelectListItem {
                            Text = s.GetTranslation(x => x.Name, request.Language.Id),
                            Value = s.Id.ToString(),
                            Selected = (s.Id == model.StateProvinceId)
                        });
                    }
                }
            }

            //form fields
            model.CompanyEnabled = _vendorSettings.CompanyEnabled;
            model.CompanyRequired = _vendorSettings.CompanyRequired;
            model.StreetAddressEnabled = _vendorSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _vendorSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _vendorSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _vendorSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _vendorSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _vendorSettings.ZipPostalCodeRequired;
            model.CityEnabled = _vendorSettings.CityEnabled;
            model.CityRequired = _vendorSettings.CityRequired;
            model.CountryEnabled = _vendorSettings.CountryEnabled;
            model.StateProvinceEnabled = _vendorSettings.StateProvinceEnabled;
            model.PhoneEnabled = _vendorSettings.PhoneEnabled;
            model.PhoneRequired = _vendorSettings.PhoneRequired;
            model.FaxEnabled = _vendorSettings.FaxEnabled;
            model.FaxRequired = _vendorSettings.FaxRequired;

            return model;
        }
    }
}
