using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
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
    private readonly IDateTimeService _dateTimeService;
    private readonly IMediator _mediator;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly OrderSettings _orderSettings;
    private readonly IPaymentService _paymentService;
    private readonly ShippingSettings _shippingSettings;
    private readonly IRequestTranslationService _translationService;

    public GetConfirmOrderHandler(
        ICountryService countryService,
        IPaymentService paymentService,
        IMediator mediator,
        OrderSettings orderSettings,
        ShippingSettings shippingSettings,
        IOrderApprovalService orderApprovalService,
        IDateTimeService dateTimeService,
        IRequestTranslationService translationService)

    {
        _countryService = countryService;
        _paymentService = paymentService;
        _mediator = mediator;
        _orderSettings = orderSettings;
        _shippingSettings = shippingSettings;
        _orderApprovalService = orderApprovalService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
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
    ///     Tells the confirm step whether the order needs an approval first; left null when it does not
    /// </summary>
    private async Task PrepareOrderApproval(CheckoutConfirmModel model, GetConfirmOrder request)
    {
        var state = await _orderApprovalService.GetCheckoutState(request.Cart, request.CalculationContext());
        if (!state.Required)
            return;

        var approval = state.Active;
        var approvalModel = new CheckoutConfirmModel.OrderApprovalStateModel {
            Required = true,
            Approved = approval?.Status == OrderApprovalStatus.Approved,
            Pending = approval?.Status == OrderApprovalStatus.Pending,
            ApprovalId = approval?.Id
        };

        if (approvalModel.Pending)
        {
            approvalModel.Message = _translationService.GetResource("OrderApproval.Confirm.Pending");
            approvalModel.PendingSince = _dateTimeService
                .ConvertToUserTime(approval.CreatedOnUtc, DateTimeKind.Utc).ToString("g");
        }
        else if (!approvalModel.Approved)
        {
            approvalModel.Message = _translationService.GetResource("OrderApproval.Confirm.Required");
        }

        model.OrderApproval = approvalModel;
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