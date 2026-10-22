using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Text;

namespace Grand.Web.AdminShared.Services;

public class CustomerAddressViewModelService(
    ICustomerService customerService,
    ICountryService countryService,
    IAddressAttributeParser addressAttributeParser,
    IAddressAttributeService addressAttributeService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    AddressSettings addressSettings)
    : ICustomerAddressViewModelService
{
    public virtual async Task<IEnumerable<AddressModel>> PrepareAddressModel(Customer customer)
    {
        var addresses = customer.Addresses.ToList();
        var addressesListModel = new List<AddressModel>();
        foreach (var x in addresses)
        {
            var model = await x.ToModel(countryService);
            var addressHtmlSb = new StringBuilder("<div>");
            if (addressSettings.CompanyEnabled && !string.IsNullOrEmpty(model.Company))
                addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.Company));
            if (addressSettings.StreetAddressEnabled && !string.IsNullOrEmpty(model.Address1))
                addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.Address1));
            if (addressSettings.StreetAddress2Enabled && !string.IsNullOrEmpty(model.Address2))
                addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.Address2));
            if (addressSettings.CityEnabled && !string.IsNullOrEmpty(model.City))
                addressHtmlSb.AppendFormat("{0},", WebUtility.HtmlEncode(model.City));
            if (addressSettings.StateProvinceEnabled && !string.IsNullOrEmpty(model.StateProvinceName))
                addressHtmlSb.AppendFormat("{0},", WebUtility.HtmlEncode(model.StateProvinceName));
            if (addressSettings.ZipPostalCodeEnabled && !string.IsNullOrEmpty(model.ZipPostalCode))
                addressHtmlSb.AppendFormat("{0}<br />", WebUtility.HtmlEncode(model.ZipPostalCode));
            if (addressSettings.CountryEnabled && !string.IsNullOrEmpty(model.CountryName))
                addressHtmlSb.AppendFormat("{0}", WebUtility.HtmlEncode(model.CountryName));
            var customAttributesFormatted =
                await addressAttributeParser.FormatAttributes(contextAccessor.WorkContext.WorkingLanguage, x.Attributes);
            if (!string.IsNullOrEmpty(customAttributesFormatted))
                //already encoded
                addressHtmlSb.AppendFormat("<br />{0}", customAttributesFormatted);
            addressHtmlSb.Append("</div>");
            model.AddressHtml = addressHtmlSb.ToString();
            addressesListModel.Add(model);
        }

        return addressesListModel;
    }

    public virtual async Task DeleteAddress(Customer customer, Address address)
    {
        customer.RemoveAddress(address);
        await customerService.UpdateCustomerInAdminPanel(customer);
    }

    public virtual async Task<Address> InsertAddressModel(Customer customer, CustomerAddressModel model,
        List<CustomAttribute> customAttributes)
    {
        var address = model.Address.ToEntity();
        address.Attributes = customAttributes;
        customer.Addresses.Add(address);
        await customerService.UpdateCustomerInAdminPanel(customer);
        return address;
    }

    public virtual async Task PrepareAddressModel(CustomerAddressModel model, Address address, Customer customer,
        bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(customer);

        model.CustomerId = customer.Id;
        if (address != null)
            if (!excludeProperties)
                model.Address = await address.ToModel(countryService);

        model.Address ??= new AddressModel();

        model.Address.NameEnabled = addressSettings.NameEnabled;
        model.Address.FirstNameEnabled = true;
        model.Address.FirstNameRequired = true;
        model.Address.LastNameEnabled = true;
        model.Address.LastNameRequired = true;
        model.Address.EmailEnabled = true;
        model.Address.EmailRequired = true;
        model.Address.CompanyEnabled = addressSettings.CompanyEnabled;
        model.Address.CompanyRequired = addressSettings.CompanyRequired;
        model.Address.VatNumberEnabled = addressSettings.VatNumberEnabled;
        model.Address.VatNumberRequired = addressSettings.VatNumberRequired;
        model.Address.CountryEnabled = addressSettings.CountryEnabled;
        model.Address.StateProvinceEnabled = addressSettings.StateProvinceEnabled;
        model.Address.CityEnabled = addressSettings.CityEnabled;
        model.Address.CityRequired = addressSettings.CityRequired;
        model.Address.StreetAddressEnabled = addressSettings.StreetAddressEnabled;
        model.Address.StreetAddressRequired = addressSettings.StreetAddressRequired;
        model.Address.StreetAddress2Enabled = addressSettings.StreetAddress2Enabled;
        model.Address.StreetAddress2Required = addressSettings.StreetAddress2Required;
        model.Address.ZipPostalCodeEnabled = addressSettings.ZipPostalCodeEnabled;
        model.Address.ZipPostalCodeRequired = addressSettings.ZipPostalCodeRequired;
        model.Address.PhoneEnabled = addressSettings.PhoneEnabled;
        model.Address.PhoneRequired = addressSettings.PhoneRequired;
        model.Address.FaxEnabled = addressSettings.FaxEnabled;
        model.Address.FaxRequired = addressSettings.FaxRequired;
        model.Address.NoteEnabled = addressSettings.NoteEnabled;
        model.Address.AddressTypeEnabled = addressSettings.AddressTypeEnabled;

        //countries
        model.Address.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.Address.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == model.Address.CountryId });
        //states
        var states = !string.IsNullOrEmpty(model.Address.CountryId)
            ? (await countryService.GetCountryById(model.Address.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.Address.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == model.Address.StateProvinceId });

        //customer attribute services
        await model.Address.PrepareCustomAddressAttributes(address, addressAttributeService, addressAttributeParser);
    }

    public virtual async Task<Address> UpdateAddressModel(Customer customer, Address address,
        CustomerAddressModel model, List<CustomAttribute> customAttributes)
    {
        address = model.Address.ToEntity(address);
        address.Attributes = customAttributes;
        await customerService.UpdateCustomerInAdminPanel(customer);
        return address;
    }
}
