using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class OrderAddressViewModelService(
    IOrderService orderService,
    ICountryService countryService,
    IAddressAttributeService addressAttributeService,
    IAddressAttributeParser addressAttributeParser,
    IRequestTranslationService translationService,
    AddressSettings addressSettings)
    : IOrderAddressViewModelService
{
    public virtual async Task<OrderAddressModel> PrepareOrderAddressModel(Order order, Address address)
    {
        var model = new OrderAddressModel {
            OrderId = order.Id,
            Address = await address.ToModel(countryService)
        };
        model.Address.Id = address.Id;
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
        model.Address.AvailableCountries.Add(new SelectListItem { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id, Selected = c.Id == address.CountryId });
        //states
        var states = !string.IsNullOrEmpty(address.CountryId)
            ? (await countryService.GetCountryById(address.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id, Selected = s.Id == address.StateProvinceId });

        //customer attribute services
        await model.Address.PrepareCustomAddressAttributes(address, addressAttributeService,
            addressAttributeParser);

        return model;
    }

    public virtual async Task<Address> UpdateOrderAddress(Order order, Address address, OrderAddressModel model,
        List<CustomAttribute> customAttributes)
    {
        address = model.Address.ToEntity(address);
        address.Attributes = customAttributes;
        await orderService.UpdateOrder(order);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = "Address has been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        return address;
    }
}
