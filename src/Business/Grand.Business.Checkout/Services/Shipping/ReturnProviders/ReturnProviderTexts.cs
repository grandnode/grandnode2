using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Common;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Services.Shipping.ReturnProviders;

/// <summary>
///     The texts the built-in return providers write for a merchandise return, in the language of its order
/// </summary>
internal class ReturnProviderTexts(
    ITranslationService translationService,
    IOrderService orderService,
    IStoreService storeService,
    ILanguageService languageService,
    ICountryService countryService)
{
    /// <param name="withRecipient">
    ///     Whether the address starts with who receives the parcel: the company, else the person's name
    /// </param>
    public async Task<string> Instructions(MerchandiseReturn merchandiseReturn, string resourceKey, Address address,
        bool withRecipient = false)
    {
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        var store = await storeService.GetStoreById(merchandiseReturn.StoreId);
        var language = await languageService.GetLanguageOrStoreDefault(order?.CustomerLanguageId, store);
        var format = translationService.GetResource(resourceKey, language?.Id ?? "");
        return string.Format(format, await OneLine(address, withRecipient), merchandiseReturn.ReturnNumber);
    }

    private async Task<string> OneLine(Address address, bool withRecipient)
    {
        if (address == null)
            return "";

        var country = string.IsNullOrEmpty(address.CountryId)
            ? null
            : await countryService.GetCountryById(address.CountryId);
        var city = string.Join(" ", new[] { address.ZipPostalCode, address.City }.Where(x => !string.IsNullOrEmpty(x)));
        var recipient = withRecipient ? Recipient(address) : null;
        return string.Join(", ",
            new[] { recipient, address.Address1, city, country?.Name }.Where(x => !string.IsNullOrEmpty(x)));
    }

    private static string Recipient(Address address)
    {
        if (!string.IsNullOrWhiteSpace(address.Company))
            return address.Company.Trim();

        return string.Join(" ",
            new[] { address.FirstName, address.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
    }
}
