using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Checkout.Services.Shipping.ReturnProviders;

/// <summary>
///     The customer sends the parcel to the return address with a carrier of their own
/// </summary>
public class SendYourselfReturnProvider : IReturnShippingProvider
{
    public const string SystemName = MerchandiseReturnShippingExtensions.SendYourselfSystemName;
    public const string OptionCode = "send";
    private const string OptionName = "MerchandiseReturns.ReturnShipping.SendYourself";
    private const string InstructionsKey = "MerchandiseReturns.ReturnShipping.SendYourself.Instructions";

    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly ReturnProviderTexts _texts;

    public SendYourselfReturnProvider(
        ISettingService settingService,
        ITranslationService translationService,
        IOrderService orderService,
        IStoreService storeService,
        ILanguageService languageService,
        ICountryService countryService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _texts = new ReturnProviderTexts(translationService, orderService, storeService, languageService,
            countryService);
    }

    string IProvider.SystemName => SystemName;
    public string FriendlyName => "Send it yourself";
    public int Priority => 0;
    public string ConfigurationUrl => "";
    public IList<string> LimitedToStores => new List<string>();
    public IList<string> LimitedToGroups => new List<string>();
    public IShipmentTracker ShipmentTracker => null;

    public async Task<IList<ReturnShippingOption>> GetReturnOptions(ReturnShippingRequest request)
    {
        var settings = await _settingService.LoadSetting<ReturnShippingProviderSettings>(request.StoreId);
        return [
            new ReturnShippingOption {
                Code = OptionCode,
                Name = _translationService.GetResource(OptionName, request.LanguageId ?? ""),
                Cost = settings.SendYourselfCost,
                HasForm = false,
                CreatesLabel = false
            }
        ];
    }

    public Task<string> GetFormRouteName(string optionCode)
    {
        return Task.FromResult("");
    }

    public Task<ReturnShippingValidationResult> ValidateForm(ReturnShippingRequest request, string optionCode,
        IDictionary<string, string> form)
    {
        return Task.FromResult(new ReturnShippingValidationResult());
    }

    public async Task<ReturnLabelResult> CreateLabel(MerchandiseReturn merchandiseReturn)
    {
        return new ReturnLabelResult {
            Success = true,
            //the parcel is addressed by the customer, so the address says who receives it
            Instructions = await _texts.Instructions(merchandiseReturn, InstructionsKey,
                merchandiseReturn.ReturnShipping?.ReturnAddress, true)
        };
    }

    public Task<bool> CancelLabel(MerchandiseReturn merchandiseReturn)
    {
        return Task.FromResult(true);
    }
}
