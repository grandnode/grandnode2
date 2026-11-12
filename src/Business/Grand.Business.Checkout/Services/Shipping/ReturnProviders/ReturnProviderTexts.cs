using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Services.Shipping.ReturnProviders;

/// <summary>
///     The texts the built-in return providers write for a merchandise return, in the language of its order
/// </summary>
/// <param name="vendorService">Names a vendor receiving the parcel; needed only for the texts with a recipient</param>
internal class ReturnProviderTexts(
    ITranslationService translationService,
    IOrderService orderService,
    IStoreService storeService,
    ILanguageService languageService,
    ICountryService countryService,
    IVendorService vendorService = null)
{
    /// <param name="withRecipient">
    ///     Whether the address starts with who receives the parcel: the company, else the person's name, else the
    ///     vendor whose address it is
    /// </param>
    public async Task<string> Instructions(MerchandiseReturn merchandiseReturn, string resourceKey, Address address,
        bool withRecipient = false)
    {
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        var store = await storeService.GetStoreById(merchandiseReturn.StoreId);
        var language = await languageService.GetLanguageOrStoreDefault(order?.CustomerLanguageId, store);
        var format = translationService.GetResource(resourceKey, language?.Id ?? "");
        var recipient = withRecipient && address != null
            ? await Recipient(merchandiseReturn.VendorId, address)
            : null;
        return string.Format(format, await OneLine(address, recipient), merchandiseReturn.ReturnNumber);
    }

    /// <summary>
    ///     The address on one line, starting with who receives the parcel, as the instructions write it
    /// </summary>
    /// <param name="vendorId">The vendor the items go back to; empty for the store</param>
    public async Task<string> AddressWithRecipient(Address address, string vendorId)
    {
        return address == null ? "" : await OneLine(address, await Recipient(vendorId, address));
    }

    private async Task<string> OneLine(Address address, string recipient)
    {
        if (address == null)
            return "";

        var country = string.IsNullOrEmpty(address.CountryId)
            ? null
            : await countryService.GetCountryById(address.CountryId);
        var city = string.Join(" ", new[] { address.ZipPostalCode, address.City }.Where(x => !string.IsNullOrEmpty(x)));
        return string.Join(", ",
            new[] { recipient, address.Address1, city, country?.Name }.Where(x => !string.IsNullOrEmpty(x)));
    }

    private async Task<string> Recipient(string vendorId, Address address)
    {
        if (!string.IsNullOrWhiteSpace(address.Company))
            return address.Company.Trim();

        var name = string.Join(" ",
            new[] { address.FirstName, address.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        if (!string.IsNullOrEmpty(name) || string.IsNullOrEmpty(vendorId) || vendorService == null)
            return name;

        //a vendor's address often names no one: the parcel goes to the vendor when the address kept with the return
        //is the vendor's, not the store's it fell back to
        var vendor = await vendorService.GetVendorById(vendorId);
        return IsAddressOf(vendor?.Address, address) ? vendor.Name?.Trim() : name;
    }

    private static bool IsAddressOf(Address vendorAddress, Address address)
    {
        return vendorAddress != null && !string.IsNullOrEmpty(address.Address1)
                                     && Same(vendorAddress.Address1, address.Address1)
                                     && Same(vendorAddress.CountryId, address.CountryId)
                                     && Same(vendorAddress.ZipPostalCode, address.ZipPostalCode);

        static bool Same(string a, string b)
        {
            return string.Equals(a?.Trim() ?? "", b?.Trim() ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}
