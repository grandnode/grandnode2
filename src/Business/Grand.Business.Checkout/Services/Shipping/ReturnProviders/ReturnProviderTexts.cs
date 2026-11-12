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
        var recipient = withRecipient && address != null ? await Recipient(merchandiseReturn, address) : null;
        return string.Format(format, await OneLine(address, recipient), merchandiseReturn.ReturnNumber);
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

    private async Task<string> Recipient(MerchandiseReturn merchandiseReturn, Address address)
    {
        if (!string.IsNullOrWhiteSpace(address.Company))
            return address.Company.Trim();

        var name = string.Join(" ",
            new[] { address.FirstName, address.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        if (!string.IsNullOrEmpty(name) || string.IsNullOrEmpty(merchandiseReturn.VendorId) || vendorService == null)
            return name;

        //a vendor's address often names no one: the parcel goes to the vendor, while the address is the vendor's and
        //not the store's it falls back to
        var vendor = await vendorService.GetVendorById(merchandiseReturn.VendorId);
        return ReturnShippingService.IsVendorReturnAddress(vendor?.Address) ? vendor.Name?.Trim() : name;
    }
}
