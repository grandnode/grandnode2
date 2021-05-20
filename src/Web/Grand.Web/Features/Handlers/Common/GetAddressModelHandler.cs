using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Common
{
    public class GetAddressModelHandler : IRequestHandler<GetAddressModel, AddressModel>
    {
        private readonly ICountryService _countryService;
        private readonly ITranslationService _translationService;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly IAddressAttributeParser _addressAttributeParser;

        private readonly AddressSettings _addressSettings;

        public GetAddressModelHandler(
            ICountryService countryService,
            ITranslationService translationService,
            IAddressAttributeService addressAttributeService,
            IAddressAttributeParser addressAttributeParser,
            AddressSettings addressSettings)
        {
            _countryService = countryService;
            _translationService = translationService;
            _addressAttributeService = addressAttributeService;
            _addressAttributeParser = addressAttributeParser;
            _addressSettings = addressSettings;
        }

        public async Task<AddressModel> Handle(GetAddressModel request, CancellationToken cancellationToken)
        {
            var model = request.Model ?? new AddressModel();

            //prepare address model
            await PrepareAddressModel(model, request.Address, request.ExcludeProperties,
                request.LoadCountries, request.PrePopulateWithCustomerFields, request.Customer, request.Language, request.Store);

            //customer attribute services
            await PrepareCustomAddressAttributes(model, request.Address, request.Language,
                request.OverrideAttributes);

            if (request.Address != null)
            {
                model.FormattedCustomAddressAttributes = await _addressAttributeParser.FormatAttributes(request.Language, request.Address.Attributes);
            }
            return model;
        }

        private async Task PrepareAddressModel(AddressModel model,
            Address address, bool excludeProperties,
            Func<IList<Country>> loadCountries = null,
            bool prePopulateWithCustomerFields = false,
            Customer customer = null,
            Language language = null,
            Store store = null)
        {
            if (!excludeProperties && address != null)
            {
                model.Id = address.Id;
                model.FirstName = address.FirstName;
                model.LastName = address.LastName;
                model.Email = address.Email;
                model.Company = address.Company;
                model.VatNumber = address.VatNumber;
                model.CountryId = address.CountryId;
                Country country = null;
                if (!String.IsNullOrEmpty(address.CountryId))
                    country = await _countryService.GetCountryById(address.CountryId);
                model.CountryName = country != null ? country.GetTranslation(x => x.Name, language.Id) : null;

                model.StateProvinceId = address.StateProvinceId;
                StateProvince state = null;
                if (!String.IsNullOrEmpty(address.StateProvinceId) && country != null)
                    state = country.StateProvinces.FirstOrDefault(x => x.Id == address.StateProvinceId);
                model.StateProvinceName = state != null ? state.GetTranslation(x => x.Name, language.Id) : null;

                model.City = address.City;
                model.Address1 = address.Address1;
                model.Address2 = address.Address2;
                model.ZipPostalCode = address.ZipPostalCode;
                model.PhoneNumber = address.PhoneNumber;
                model.FaxNumber = address.FaxNumber;
                model.Note = address.Note;
                model.AddressTypeId = (int)address.AddressType;
            }

            if (address == null && prePopulateWithCustomerFields)
            {
                if (customer == null)
                    throw new Exception("Customer cannot be null when prepopulating an address");
                model.Email = customer.Email;
                model.FirstName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);
                model.LastName = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);
                model.Company = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company);
                model.VatNumber = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber);
                model.Address1 = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress);
                model.Address2 = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2);
                model.ZipPostalCode = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode);
                model.City = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City);
                model.CountryId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId);
                model.StateProvinceId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId);
                model.PhoneNumber = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone);
                model.FaxNumber = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax);
            }

            //countries and states
            if (_addressSettings.CountryEnabled && loadCountries != null)
            {

                model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectCountry"), Value = "" });
                foreach (var c in loadCountries())
                {
                    model.AvailableCountries.Add(new SelectListItem {
                        Text = c.GetTranslation(x => x.Name, language.Id),
                        Value = c.Id.ToString(),
                        Selected = !string.IsNullOrEmpty(model.CountryId) ? c.Id == model.CountryId : (c.Id == store.DefaultCountryId)
                    });
                }

                if (_addressSettings.StateProvinceEnabled)
                {
                    var states = await _countryService
                        .GetStateProvincesByCountryId(!string.IsNullOrEmpty(model.CountryId) ? model.CountryId : store.DefaultCountryId, language.Id);

                    model.AvailableStates.Add(new SelectListItem { Text = _translationService.GetResource("Address.SelectState"), Value = "" });

                    foreach (var s in states)
                    {
                        model.AvailableStates.Add(new SelectListItem {
                            Text = s.GetTranslation(x => x.Name, language.Id),
                            Value = s.Id.ToString(),
                            Selected = (s.Id == model.StateProvinceId)
                        });
                    }
                }
            }

            //form fields
            model.CompanyEnabled = _addressSettings.CompanyEnabled;
            model.CompanyRequired = _addressSettings.CompanyRequired;
            model.VatNumberEnabled = _addressSettings.VatNumberEnabled;
            model.VatNumberRequired = _addressSettings.VatNumberRequired;
            model.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _addressSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _addressSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
            model.CityEnabled = _addressSettings.CityEnabled;
            model.CityRequired = _addressSettings.CityRequired;
            model.CountryEnabled = _addressSettings.CountryEnabled;
            model.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
            model.PhoneEnabled = _addressSettings.PhoneEnabled;
            model.PhoneRequired = _addressSettings.PhoneRequired;
            model.FaxEnabled = _addressSettings.FaxEnabled;
            model.FaxRequired = _addressSettings.FaxRequired;
            model.NoteEnabled = _addressSettings.NoteEnabled;
            model.AddressTypeEnabled = _addressSettings.AddressTypeEnabled;
        }

        private async Task PrepareCustomAddressAttributes(AddressModel model, Address address,
            Language language, IList<CustomAttribute> overrideAttributes)
        {

            var attributes = await _addressAttributeService.GetAllAddressAttributes();
            foreach (var attribute in attributes)
            {
                var attributeModel = new AddressAttributeModel {
                    Id = attribute.Id,
                    Name = attribute.GetTranslation(x => x.Name, language.Id),
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.AddressAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new AddressAttributeValueModel {
                            Id = attributeValue.Id,
                            Name = attributeValue.GetTranslation(x => x.Name, language.Id),
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(attributeValueModel);
                    }
                }

                //set already selected attributes
                var selectedAddressAttributes = overrideAttributes ?? (address?.Attributes);

                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Checkboxes:
                        {
                            if (selectedAddressAttributes != null)
                            {
                                //clear default selection
                                foreach (var item in attributeModel.Values)
                                    item.IsPreSelected = false;

                                //select new values
                                var selectedValues = await _addressAttributeParser.ParseAddressAttributeValues(selectedAddressAttributes);
                                foreach (var attributeValue in selectedValues)
                                    if (attributeModel.Id == attributeValue.AddressAttributeId)
                                        foreach (var item in attributeModel.Values)
                                            if (attributeValue.Id == item.Id)
                                                item.IsPreSelected = true;
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //do nothing
                            //values are already pre-set
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            if (selectedAddressAttributes != null)
                            {
                                var enteredText = selectedAddressAttributes.Where(x => x.Key == attribute.Id).Select(x => x.Value).ToList();
                                if (enteredText.Any())
                                    attributeModel.DefaultValue = enteredText[0];
                            }
                        }
                        break;
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                    case AttributeControlType.Datepicker:
                    case AttributeControlType.FileUpload:
                    default:
                        //not supported attribute control types
                        break;
                }

                model.CustomAddressAttributes.Add(attributeModel);
            }
        }
    }
}
