using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Messages.Commands.Handlers.Messages;

public class
    GetMerchandiseReturnTokensCommandHandler : IRequestHandler<GetMerchandiseReturnTokensCommand,
    LiquidMerchandiseReturn>
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;

    public GetMerchandiseReturnTokensCommandHandler(
        ICountryService countryService,
        IProductService productService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter)
    {
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _countryService = countryService;
        _productService = productService;
    }

    public async Task<LiquidMerchandiseReturn> Handle(GetMerchandiseReturnTokensCommand request,
        CancellationToken cancellationToken)
    {
        var liquidMerchandiseReturn = new LiquidMerchandiseReturn(request.MerchandiseReturn, request.Store,
            request.Host, request.Order, request.MerchandiseReturnNote);

        var country = await _countryService.GetCountryById(request.MerchandiseReturn.PickupAddress.CountryId);

        liquidMerchandiseReturn.Status = request.MerchandiseReturn.MerchandiseReturnStatus.ToString();

        liquidMerchandiseReturn.PickupAddressStateProvince =
            !string.IsNullOrEmpty(request.MerchandiseReturn.PickupAddress.StateProvinceId)
                ? country?.StateProvinces
                    .FirstOrDefault(x => x.Id == request.MerchandiseReturn.PickupAddress.StateProvinceId)
                    ?.GetTranslation(x => x.Name, request.Language.Id)
                : "";

        liquidMerchandiseReturn.PickupAddressCountry =
            !string.IsNullOrEmpty(request.MerchandiseReturn.PickupAddress.CountryId)
                ? country?.GetTranslation(x => x.Name, request.Language.Id)
                : "";

        var shipping = request.MerchandiseReturn.ReturnShipping;
        if (shipping != null)
        {
            var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
            //texts the customer, the staff or a provider wrote go into the HTML of the message encoded, as the
            //customer's comment does
            liquidMerchandiseReturn.ReturnMethod = FormatText.ConvertText(shipping.OptionName);
            liquidMerchandiseReturn.ReturnCost = _priceFormatter.FormatPrice(shipping.Cost * shipping.CurrencyRate, currency);
            liquidMerchandiseReturn.ReturnTrackingNumber = FormatText.ConvertText(shipping.TrackingNumber);
            liquidMerchandiseReturn.ReturnCode = FormatText.ConvertText(shipping.ReturnCode);
            liquidMerchandiseReturn.ReturnInstructions = FormatText.ConvertText(shipping.Instructions);
            liquidMerchandiseReturn.HasReturnLabel = shipping.LabelStatus == ReturnLabelStatus.Created;
        }

        foreach (var merchandiseReturnItem in request.MerchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = request.Order.OrderItems.Where(x => x.Id == merchandiseReturnItem.OrderItemId).First();
            var product = await _productService.GetProductById(orderItem.ProductId);
            var liquidItem = new LiquidMerchandiseReturnItem(merchandiseReturnItem, orderItem, product,
                request.Order.CustomerLanguageId);

            liquidMerchandiseReturn.Items.Add(liquidItem);
        }

        return liquidMerchandiseReturn;
    }
}