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
    private readonly IOrderPlacementService _orderPlacementService;
    private readonly IRequestTranslationService _translationService;
    private readonly IMediator _mediator;
    private readonly OrderSettings _orderSettings;
    private readonly IPaymentService _paymentService;
    private readonly ShippingSettings _shippingSettings;

    public GetConfirmOrderHandler(
        ICountryService countryService,
        IPaymentService paymentService,
        IMediator mediator,
        IOrderPlacementService orderPlacementService,
        IRequestTranslationService translationService,
        OrderSettings orderSettings,
        ShippingSettings shippingSettings)
    {
        _countryService = countryService;
        _paymentService = paymentService;
        _mediator = mediator;
        _orderPlacementService = orderPlacementService;
        _translationService = translationService;
        _orderSettings = orderSettings;
        _shippingSettings = shippingSettings;
    }

    public async Task<CheckoutConfirmModel> Handle(GetConfirmOrder request, CancellationToken cancellationToken)
    {
        var model = new CheckoutConfirmModel {
            //terms of service
            TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage,
            PurchaseOrderNumberMode = GetPurchaseOrderNumberMode(request),
            PurchaseOrderNumberMaxLength = PurchaseOrderNumberRules.MaxLength
        };
        await PrepareOrderReviewData(model, request);
        await PrepareOrderPlacement(model, request);
        return model;
    }

    /// <summary>
    ///     The chosen payment method decides whether the buyer is asked for a purchase order number
    /// </summary>
    private PurchaseOrderNumberMode GetPurchaseOrderNumberMode(GetConfirmOrder request)
    {
        var paymentMethod = request.Customer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
        return _paymentService.LoadPaymentMethodBySystemName(paymentMethod)?.PurchaseOrderNumberMode
               ?? PurchaseOrderNumberMode.Disabled;
    }

    /// <summary>
    ///     A blocked order cannot be confirmed, and one that needs an approval is placed at once and waits for it;
    ///     the buyer is told so, and why
    /// </summary>
    private async Task PrepareOrderPlacement(CheckoutConfirmModel model, GetConfirmOrder request)
    {
        var paymentMethod = request.Customer.GetUserFieldFromEntity<string>(
            SystemCustomerFieldNames.SelectedPaymentMethod, request.Store.Id);
        var evaluation = await _orderPlacementService.Evaluate(request.Cart,
            new CalculationContext(request.Customer, request.Store, request.Currency, request.TaxDisplayType),
            request.Language, paymentMethod);

        if (evaluation.Decision == OrderPlacementDecision.Block)
        {
            model.OrderPlacementBlocked = true;
            model.OrderPlacementBlockedMessage = string.Join(" ", evaluation.Reasons);
        }
        else if (evaluation.Decision == OrderPlacementDecision.RequireApproval)
        {
            model.OrderApprovalRequired = true;
            model.OrderApprovalMessage = string.Join(" ",
                evaluation.Reasons.Prepend(_translationService.GetResource("Checkout.OrderApproval.WillBeSentGeneric")));
            model.OrderApprovalButtonText = _translationService.GetResource("Checkout.OrderApproval.SendButton");
        }
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