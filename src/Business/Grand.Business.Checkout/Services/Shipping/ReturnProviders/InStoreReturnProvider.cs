using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Checkout.Services.Shipping.ReturnProviders;

/// <summary>
///     The customer brings the items to a pickup point of the store; offered for the store's own items only
/// </summary>
public class InStoreReturnProvider : IReturnShippingProvider
{
    public const string SystemName = "ReturnShipping.InStore";
    public const string OptionCode = "instore";
    public const string PickupPointField = "pickupPointId";
    public const string FormRouteName = "MerchandiseReturnInStorePoints";
    private const string OptionName = "MerchandiseReturns.ReturnShipping.InStore";
    private const string ChoosePointKey = "MerchandiseReturns.ReturnShipping.InStore.ChoosePoint";
    private const string InstructionsKey = "MerchandiseReturns.ReturnShipping.InStore.Instructions";

    private readonly IPickupPointService _pickupPointService;
    private readonly ITranslationService _translationService;
    private readonly ReturnProviderTexts _texts;

    public InStoreReturnProvider(
        IPickupPointService pickupPointService,
        ITranslationService translationService,
        IOrderService orderService,
        IStoreService storeService,
        ILanguageService languageService,
        ICountryService countryService)
    {
        _pickupPointService = pickupPointService;
        _translationService = translationService;
        _texts = new ReturnProviderTexts(translationService, orderService, storeService, languageService,
            countryService);
    }

    string IProvider.SystemName => SystemName;
    public string FriendlyName => "Return in store";
    public int Priority => 1;
    public string ConfigurationUrl => "";
    public IList<string> LimitedToStores => new List<string>();
    public IList<string> LimitedToGroups => new List<string>();
    public IShipmentTracker ShipmentTracker => null;

    public async Task<IList<ReturnShippingOption>> GetReturnOptions(ReturnShippingRequest request)
    {
        //the items of a vendor go back to the vendor, not to the store
        if (!string.IsNullOrEmpty(request.VendorId))
            return [];

        if ((await _pickupPointService.LoadActivePickupPoints(request.StoreId)).Count == 0)
            return [];

        return [
            new ReturnShippingOption {
                Code = OptionCode,
                Name = _translationService.GetResource(OptionName, request.LanguageId ?? ""),
                Cost = 0,
                HasForm = true,
                CreatesLabel = false
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
        //only the active points of the store: a posted id of another store's point is not accepted
        var pointId = form != null && form.TryGetValue(PickupPointField, out var posted) ? posted : null;
        var point = string.IsNullOrEmpty(pointId)
            ? null
            : (await _pickupPointService.LoadActivePickupPoints(request.StoreId)).FirstOrDefault(x =>
                x.Id == pointId);
        if (point == null)
        {
            result.Errors.Add(_translationService.GetResource(ChoosePointKey, request.LanguageId ?? ""));
            return result;
        }

        result.DropOffPoint = new ReturnDropOffPoint {
            Id = point.Id,
            Name = point.Name,
            Address = point.Address,
            WarehouseId = point.WarehouseId
        };
        return result;
    }

    public async Task<ReturnLabelResult> CreateLabel(MerchandiseReturn merchandiseReturn)
    {
        return new ReturnLabelResult {
            Success = true,
            Instructions = await _texts.Instructions(merchandiseReturn, InstructionsKey,
                merchandiseReturn.ReturnShipping?.DropOffPoint?.Address)
        };
    }

    public Task<bool> CancelLabel(MerchandiseReturn merchandiseReturn)
    {
        return Task.FromResult(true);
    }
}
