using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Common.Localization;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Checkout;
using Grand.Web.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Checkout;

public class GetConfirmOrderHandler : IRequestHandler<GetConfirmOrder, CheckoutConfirmModel>
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IRequestTranslationService _translationService;
    private readonly IMediator _mediator;
    private readonly OrderSettings _orderSettings;
    private readonly IPaymentService _paymentService;
    private readonly ShippingSettings _shippingSettings;

    public GetConfirmOrderHandler(
        ICountryService countryService,
        IPaymentService paymentService,
        IMediator mediator,
        IOrderApprovalService orderApprovalService,
        ICurrencyService currencyService,
        IPriceFormatter priceFormatter,
        IRequestTranslationService translationService,
        OrderSettings orderSettings,
        ShippingSettings shippingSettings)
    {
        _countryService = countryService;
        _paymentService = paymentService;
        _mediator = mediator;
        _orderApprovalService = orderApprovalService;
        _currencyService = currencyService;
        _priceFormatter = priceFormatter;
        _translationService = translationService;
        _orderSettings = orderSettings;
        _shippingSettings = shippingSettings;
    }

    public async Task<CheckoutConfirmModel> Handle(GetConfirmOrder request, CancellationToken cancellationToken)
    {
        var model = new CheckoutConfirmModel {
            //terms of service
            TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage
        };
        await PrepareOrderReviewData(model, request);
        await PrepareOrderApproval(model, request);
        return model;
    }

    /// <summary>
    ///     An order over the buyer's limit is placed at once and waits for an approval; the buyer is told so
    /// </summary>
    private async Task PrepareOrderApproval(CheckoutConfirmModel model, GetConfirmOrder request)
    {
        var state = await _orderApprovalService.RequiresApproval(request.Cart,
            new CalculationContext(request.Customer, request.Store, request.Currency, request.TaxDisplayType));
        if (!state.Required)
            return;

        model.OrderApprovalRequired = true;
        var limit = await _currencyService.ConvertFromPrimaryStoreCurrency(state.Limit ?? 0, request.Currency);
        model.OrderApprovalMessage = string.Format(_translationService.GetResource("Checkout.OrderApproval.WillBeSent"),
            _priceFormatter.FormatPrice(limit, request.Currency));
        model.OrderApprovalButtonText = _translationService.GetResource("Checkout.OrderApproval.SendButton");
    }

    private async Task PrepareOrderReviewData(CheckoutConfirmModel model, GetConfirmOrder request)
    {
        //billing info
        var billingAddress = request.Customer.BillingAddress;
        if (billingAddress != null)
            model.OrderReviewData.BillingAddress = await _mediator.Send(new GetAddressModel {
                Language = request.Language,
                Address = billingAddress,
                ExcludeProperties = false
            });
        //shipping info
        if (request.Cart.RequiresShipping())
        {
            model.OrderReviewData.IsShippable = true;

            var pickupPoint =
                request.Customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.SelectedPickupPoint,
                    request.Store.Id);

            model.OrderReviewData.SelectedPickUpInStore =
                _shippingSettings.AllowPickUpInStore && !string.IsNullOrEmpty(pickupPoint);

            if (!model.OrderReviewData.SelectedPickUpInStore)
            {
                var shippingAddress = request.Customer.ShippingAddress;
                if (shippingAddress != null)
                    model.OrderReviewData.ShippingAddress = await _mediator.Send(new GetAddressModel {
                        Language = request.Language,
                        Address = shippingAddress,
                        ExcludeProperties = false
                    });
            }
            else
            {
                var pickup = await _mediator.Send(new GetPickupPointById { Id = pickupPoint });
                if (pickup != null)
                {
                    var country = await _countryService.GetCountryById(pickup.Address.CountryId);
                    model.OrderReviewData.PickupAddress = new AddressModel {
                        Address1 = pickup.Address.Address1,
                        City = pickup.Address.City,
                        CountryName = country != null ? country.Name : string.Empty,
                        ZipPostalCode = pickup.Address.ZipPostalCode
                    };
                }
            }

            //selected shipping method
            var shippingOption =
                request.Customer.GetUserFieldFromEntity<ShippingOption>(SystemCustomerFieldNames.SelectedShippingOption,
                    request.Store.Id);
            if (shippingOption != null)
            {
                model.OrderReviewData.ShippingMethod = shippingOption.Name;
                model.OrderReviewData.ShippingAdditionDescription =
                    request.Customer.GetUserFieldFromEntity<string>(
                        SystemCustomerFieldNames.ShippingOptionAttributeDescription, request.Store.Id);
            }
        }

        //payment info
        var selectedPaymentMethodSystemName = request.Customer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
        var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(selectedPaymentMethodSystemName);
        model.OrderReviewData.PaymentMethod = paymentMethod != null ? paymentMethod.FriendlyName : "";
    }
}