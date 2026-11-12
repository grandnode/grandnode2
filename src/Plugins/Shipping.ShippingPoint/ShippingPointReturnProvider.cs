using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.Common.Localization;
using Shipping.ShippingPoint.Services;
using System.Security.Cryptography;

namespace Shipping.ShippingPoint;

/// <summary>
///     The customer drops the items of a merchandise return off at a Shipping Point that accepts returns
/// </summary>
public class ShippingPointReturnProvider(
    IShippingPointService shippingPointService,
    ITranslationService translationService,
    IRequestTranslationService requestTranslationService,
    IOrderService orderService,
    IStoreService storeService,
    ILanguageService languageService,
    ICountryService countryService,
    ShippingPointRateSettings shippingPointRateSettings) : IReturnShippingProvider
{
    public const string OptionCode = "point";
    public const string PointField = "shippingPointId";
    public const string FormRouteName = "Plugins.ShippingPoint.ReturnPoints";
    private const string OptionNameKey = "Shipping.ShippingPoint.Return.OptionName";
    private const string ChoosePointKey = "Shipping.ShippingPoint.Return.ChoosePoint";
    private const string InstructionsKey = "Shipping.ShippingPoint.Return.Instructions";
    private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    public string SystemName => ShippingPointRateDefaults.ReturnProviderSystemName;
    public string FriendlyName => requestTranslationService.GetResource("Shipping.ShippingPoint.Return.FriendlyName");
    public int Priority => shippingPointRateSettings.DisplayOrder;
    public string ConfigurationUrl => ShippingPointRateDefaults.ConfigurationUrl;
    public IList<string> LimitedToStores => new List<string>();
    public IList<string> LimitedToGroups => new List<string>();
    public IShipmentTracker ShipmentTracker => null;

    public async Task<IList<ReturnShippingOption>> GetReturnOptions(ReturnShippingRequest request)
    {
        //the points are the store's own places, so the items of a vendor are not sent back to them
        if (!string.IsNullOrEmpty(request.VendorId))
            return [];

        var points = await shippingPointService.GetReturnShippingPoints(request.StoreId);
        if (points.Count == 0)
            return [];

        var fees = points.Select(x => Fee(x.ReturnFee)).ToList();
        return [
            new ReturnShippingOption {
                Code = OptionCode,
                Name = translationService.GetResource(OptionNameKey, request.LanguageId ?? ""),
                //the cost is final once the customer has chosen a point
                Cost = fees.Min(),
                CostVaries = fees.Distinct().Count() > 1,
                HasForm = true,
                CreatesLabel = true
            }
        ];
    }

    public Task<string> GetFormRouteName(string optionCode)
    {
        return Task.FromResult(FormRouteName);
    }

    public async Task<ReturnShippingValidationResult> ValidateForm(ReturnShippingRequest request, string optionCode,
        IDictionary<string, string> form)
    {
        var result = new ReturnShippingValidationResult();
        //only the return points of the store: a posted id of another store's point is not accepted
        var pointId = form != null && form.TryGetValue(PointField, out var posted) ? posted : null;
        var point = string.IsNullOrEmpty(pointId)
            ? null
            : (await shippingPointService.GetReturnShippingPoints(request.StoreId)).FirstOrDefault(x =>
                x.Id == pointId);
        if (point == null)
        {
            result.Errors.Add(translationService.GetResource(ChoosePointKey, request.LanguageId ?? ""));
            return result;
        }

        result.Cost = Fee(point.ReturnFee);
        result.DropOffPoint = new ReturnDropOffPoint {
            Id = point.Id,
            Name = point.ShippingPointName,
            Address = new Address {
                Address1 = point.Address1,
                City = point.City,
                ZipPostalCode = point.ZipPostalCode,
                CountryId = point.CountryId
            },
            OpeningHours = point.OpeningHours
        };
        return result;
    }

    public async Task<ReturnLabelResult> CreateLabel(MerchandiseReturn merchandiseReturn)
    {
        var code = $"{merchandiseReturn.ReturnNumber}-{RandomNumberGenerator.GetString(CodeAlphabet, CodeLength)}";

        //runs from the admin panels too, so the language is the one of the order, not of the request
        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        var store = await storeService.GetStoreById(merchandiseReturn.StoreId);
        var language = await languageService.GetLanguageOrStoreDefault(order?.CustomerLanguageId, store);
        var format = translationService.GetResource(InstructionsKey, language?.Id ?? "");

        return new ReturnLabelResult {
            Success = true,
            ReturnCode = code,
            Instructions = string.Format(format, code, await Place(merchandiseReturn.ReturnShipping?.DropOffPoint))
        };
    }

    public Task<bool> CancelLabel(MerchandiseReturn merchandiseReturn)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    ///     A negative fee typed in by mistake is no fee: the customer is never paid for a return
    /// </summary>
    private static double Fee(double returnFee)
    {
        return Math.Max(0, returnFee);
    }

    private async Task<string> Place(ReturnDropOffPoint point)
    {
        if (point == null)
            return "";

        var address = point.Address;
        var country = string.IsNullOrEmpty(address?.CountryId)
            ? null
            : await countryService.GetCountryById(address.CountryId);
        var city = string.Join(" ",
            new[] { address?.ZipPostalCode, address?.City }.Where(x => !string.IsNullOrEmpty(x)));
        return string.Join(", ",
            new[] { point.Name, address?.Address1, city, country?.Name }.Where(x => !string.IsNullOrEmpty(x)));
    }
}
