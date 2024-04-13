using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Media;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Handlers.Orders;

public class GetOrderDetailsHandler : IRequestHandler<GetOrderDetails, OrderDetailsModel>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly ICurrencyService _currencyService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly OrderSettings _orderSettings;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly PdfSettings _pdfSettings;
    private readonly IPictureService _pictureService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;
    private readonly TaxSettings _taxSettings;
    private readonly ITranslationService _translationService;

    private Currency _orderCurrency;

    public GetOrderDetailsHandler(
        IDateTimeService dateTimeService,
        IProductService productService,
        ITranslationService translationService,
        IShipmentService shipmentService,
        IPaymentService paymentService,
        IPaymentTransactionService paymentTransactionService,
        IPriceFormatter priceFormatter,
        IGiftVoucherService giftVoucherService,
        IOrderService orderService,
        IPictureService pictureService,
        IOrderStatusService orderStatusService,
        ICurrencyService currencyService,
        IMediator mediator,
        CatalogSettings catalogSettings,
        OrderSettings orderSettings,
        PdfSettings pdfSettings,
        TaxSettings taxSettings)
    {
        _dateTimeService = dateTimeService;
        _productService = productService;
        _translationService = translationService;
        _shipmentService = shipmentService;
        _paymentService = paymentService;
        _paymentTransactionService = paymentTransactionService;
        _priceFormatter = priceFormatter;
        _giftVoucherService = giftVoucherService;
        _orderService = orderService;
        _pictureService = pictureService;
        _orderStatusService = orderStatusService;
        _currencyService = currencyService;
        _mediator = mediator;
        _orderSettings = orderSettings;
        _catalogSettings = catalogSettings;
        _pdfSettings = pdfSettings;
        _taxSettings = taxSettings;
    }

    public async Task<OrderDetailsModel> Handle(GetOrderDetails request, CancellationToken cancellationToken)
    {
        _orderCurrency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
        var model = new OrderDetailsModel {
            Id = request.Order.Id,
            OrderNumber = request.Order.OrderNumber,
            OrderCode = request.Order.Code,
            CreatedOn = _dateTimeService.ConvertToUserTime(request.Order.CreatedOnUtc, DateTimeKind.Utc),
            OrderStatus = (await _orderStatusService.GetByStatusId(request.Order.OrderStatusId))?.Name,
            IsReOrderAllowed = _orderSettings.IsReOrderAllowed,
            IsMerchandiseReturnAllowed =
                await _mediator.Send(new IsMerchandiseReturnAllowedQuery { Order = request.Order }, cancellationToken),
            PdfInvoiceDisabled = _pdfSettings.DisablePdfInvoicesForPendingOrders &&
                                 request.Order.OrderStatusId == (int)OrderStatusSystem.Pending,
            ShowAddOrderNote = _orderSettings.AllowCustomerToAddOrderNote
        };

        //shipping info
        await PrepareShippingInfo(request, model);

        //billing info
        model.BillingAddress = await _mediator.Send(new GetAddressModel {
            Language = request.Language,
            Model = null,
            Address = request.Order.BillingAddress,
            ExcludeProperties = false
        }, cancellationToken);

        //VAT number
        model.VatNumber = request.Order.VatNumber;

        //payment method
        await PreparePaymentMethod(request, model);

        //order subtotal
        await PrepareOrderTotal(request, model);

        //tax
        await PrepareTax(request, model);

        //discount (applied to order total)
        await PrepareDiscount(request, model);

        //gift vouchers
        await PrepareGiftVouchers(request, model);

        //loyalty points           
        await PrepareLoyaltyPoints(request, model);

        //checkout attributes
        model.CheckoutAttributeInfo = request.Order.CheckoutAttributeDescription;

        //order notes
        await PrepareOrderNotes(request, model);

        //allow cancel order
        if (_orderSettings.UserCanCancelUnpaidOrder)
            if (request.Order.OrderStatusId == (int)OrderStatusSystem.Pending && request.Order.PaymentStatusId ==
                                                                              PaymentStatus.Pending
                                                                              && request.Order.ShippingStatusId is
                                                                                  ShippingStatus.ShippingNotRequired
                                                                                  or ShippingStatus.Pending)
                model.UserCanCancelUnpaidOrder = true;

        //purchased products
        await PrepareOrderItems(request, model);
        return model;
    }

    private async Task PrepareShippingInfo(GetOrderDetails request, OrderDetailsModel model)
    {
        model.ShippingStatus =
            request.Order.ShippingStatusId.GetTranslationEnum(_translationService, request.Language.Id);
        if (request.Order.ShippingStatusId != ShippingStatus.ShippingNotRequired)
        {
            model.IsShippable = true;
            model.PickUpInStore = request.Order.PickUpInStore;
            if (!request.Order.PickUpInStore)
            {
                model.ShippingAddress = await _mediator.Send(new GetAddressModel {
                    Language = request.Language,
                    Model = null,
                    Address = request.Order.ShippingAddress,
                    ExcludeProperties = false
                });
            }
            else
            {
                if (request.Order.PickupPoint?.Address != null)
                    model.PickupAddress = await _mediator.Send(new GetAddressModel {
                        Language = request.Language,
                        Address = request.Order.PickupPoint.Address,
                        ExcludeProperties = false
                    });
            }

            model.ShippingMethod = request.Order.ShippingMethod;
            model.ShippingAdditionDescription = request.Order.ShippingOptionAttributeDescription;
            //shipments (only already shipped)
            var shipments = (await _shipmentService.GetShipmentsByOrder(request.Order.Id))
                .Where(x => x.ShippedDateUtc.HasValue).OrderBy(x => x.CreatedOnUtc).ToList();
            foreach (var shipment in shipments)
            {
                var shipmentModel = new OrderDetailsModel.ShipmentBriefModel {
                    Id = shipment.Id,
                    ShipmentNumber = shipment.ShipmentNumber,
                    TrackingNumber = shipment.TrackingNumber
                };
                if (shipment.ShippedDateUtc.HasValue)
                    shipmentModel.ShippedDate =
                        _dateTimeService.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                if (shipment.DeliveryDateUtc.HasValue)
                    shipmentModel.DeliveryDate =
                        _dateTimeService.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
                model.Shipments.Add(shipmentModel);
            }
        }
    }

    private async Task PreparePaymentMethod(GetOrderDetails request, OrderDetailsModel model)
    {
        var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName);
        model.PaymentMethod =
            paymentMethod != null ? paymentMethod.FriendlyName : request.Order.PaymentMethodSystemName;
        model.PaymentMethodStatus =
            request.Order.PaymentStatusId.GetTranslationEnum(_translationService, request.Language.Id);
        var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(request.Order.OrderGuid);
        model.CanRePostProcessPayment = paymentTransaction != null &&
                                        await _paymentService.CanRePostRedirectPayment(paymentTransaction);
    }

    private Task PrepareOrderTotal(GetOrderDetails request, OrderDetailsModel model)
    {
        model.OrderIncludingTax = request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
        if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax &&
            !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
        {
            //including tax
            //order subtotal
            model.OrderSubtotal = _priceFormatter.FormatPrice(request.Order.OrderSubtotalInclTax, _orderCurrency);
            //discount (applied to order subtotal)
            if (request.Order.OrderSubTotalDiscountInclTax > 0)
                model.OrderSubTotalDiscount =
                    _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountInclTax, _orderCurrency);
        }
        else
        {
            //excluding tax
            //order subtotal
            model.OrderSubtotal = _priceFormatter.FormatPrice(request.Order.OrderSubtotalExclTax, _orderCurrency);
            //discount (applied to order subtotal)
            if (request.Order.OrderSubTotalDiscountExclTax > 0)
                model.OrderSubTotalDiscount =
                    _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountExclTax, _orderCurrency);
        }

        if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
        {
            //including tax
            //order shipping
            model.OrderShipping = _priceFormatter.FormatPrice(request.Order.OrderShippingInclTax, _orderCurrency);
            //payment method additional fee
            if (request.Order.PaymentMethodAdditionalFeeInclTax > 0)
                model.PaymentMethodAdditionalFee =
                    _priceFormatter.FormatPrice(request.Order.PaymentMethodAdditionalFeeInclTax, _orderCurrency);
        }
        else
        {
            //excluding tax
            //order shipping
            model.OrderShipping = _priceFormatter.FormatPrice(request.Order.OrderShippingExclTax, _orderCurrency);
            //payment method additional fee
            if (request.Order.PaymentMethodAdditionalFeeExclTax > 0)
                model.PaymentMethodAdditionalFee =
                    _priceFormatter.FormatPrice(request.Order.PaymentMethodAdditionalFeeExclTax, _orderCurrency);
        }

        //total
        model.OrderTotal = _priceFormatter.FormatPrice(request.Order.OrderTotal, _orderCurrency);
        return Task.CompletedTask;
    }

    private async Task PrepareTax(GetOrderDetails request, OrderDetailsModel model)
    {
        var displayTax = true;
        var displayTaxRates = true;
        var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
        if (_taxSettings.HideTaxInOrderSummary && request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
        {
            displayTax = false;
            displayTaxRates = false;
        }
        else
        {
            if (request.Order.OrderTax == 0 && _taxSettings.HideZeroTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                displayTaxRates = _taxSettings.DisplayTaxRates && request.Order.OrderTaxes.Any();
                displayTax = !displayTaxRates;

                model.Tax = _priceFormatter.FormatPrice(request.Order.OrderTax, currency);

                foreach (var tr in request.Order.OrderTaxes)
                    model.TaxRates.Add(new OrderDetailsModel.TaxRate {
                        Rate = _priceFormatter.FormatTaxRate(tr.Percent),
                        Value = _priceFormatter.FormatPrice(tr.Amount, currency)
                    });
            }
        }

        model.DisplayTaxRates = displayTaxRates;
        model.DisplayTax = displayTax;
        model.PricesIncludeTax = request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
    }

    private Task PrepareDiscount(GetOrderDetails request, OrderDetailsModel model)
    {
        if (request.Order.OrderDiscount > 0)
            model.OrderTotalDiscount = _priceFormatter.FormatPrice(-request.Order.OrderDiscount, _orderCurrency);

        return Task.CompletedTask;
    }

    private async Task PrepareGiftVouchers(GetOrderDetails request, OrderDetailsModel model)
    {
        foreach (var gcuh in await _giftVoucherService.GetAllGiftVoucherUsageHistory(request.Order.Id))
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
            model.GiftVouchers.Add(new OrderDetailsModel.GiftVoucher {
                CouponCode = giftVoucher.Code,
                Amount = _priceFormatter.FormatPrice(-gcuh.UsedValue, _orderCurrency)
            });
        }
    }

    private Task PrepareLoyaltyPoints(GetOrderDetails request, OrderDetailsModel model)
    {
        if (request.Order.RedeemedLoyaltyPoints > 0)
        {
            model.RedeemedLoyaltyPoints = request.Order.RedeemedLoyaltyPoints;
            model.RedeemedLoyaltyPointsAmount =
                _priceFormatter.FormatPrice(-request.Order.RedeemedLoyaltyPointsAmount, _orderCurrency);
        }

        return Task.CompletedTask;
    }

    private async Task PrepareOrderNotes(GetOrderDetails request, OrderDetailsModel model)
    {
        foreach (var orderNote in (await _orderService.GetOrderNotes(request.Order.Id))
                 .Where(on => on.DisplayToCustomer)
                 .OrderByDescending(on => on.CreatedOnUtc)
                 .ToList())
            model.OrderNotes.Add(new OrderDetailsModel.OrderNote {
                Id = orderNote.Id,
                OrderId = orderNote.OrderId,
                HasDownload = !string.IsNullOrEmpty(orderNote.DownloadId),
                Note = orderNote.Note,
                CreatedOn = _dateTimeService.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc)
            });
    }

    private async Task PrepareOrderItems(GetOrderDetails request, OrderDetailsModel model)
    {
        model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;
        foreach (var orderItem in request.Order.OrderItems)
        {
            var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
            var orderItemModel = new OrderDetailsModel.OrderItemModel {
                Id = orderItem.Id,
                OrderItemGuid = orderItem.OrderItemGuid,
                Sku = product.FormatSku(orderItem.Attributes),
                ProductId = product.Id,
                ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                ProductSeName = product.SeName,
                Quantity = orderItem.Quantity,
                AttributeInfo = orderItem.AttributeDescription
            };
            //prepare picture
            orderItemModel.Picture =
                await PrepareOrderItemPicture(product, orderItem.Attributes, orderItemModel.ProductName);


            orderItemModel.UnitPriceIncludingTax =
                request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
            //unit price, subtotal
            if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
            {
                //including tax
                orderItemModel.UnitPrice = _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax, _orderCurrency);
                orderItemModel.UnitPriceValue = orderItem.UnitPriceInclTax;

                orderItemModel.UnitPriceWithoutDiscount =
                    _priceFormatter.FormatPrice(orderItem.UnitPriceWithoutDiscInclTax, _orderCurrency);
                orderItemModel.UnitPriceWithoutDiscountValue = orderItem.UnitPriceWithoutDiscInclTax;

                orderItemModel.SubTotal = _priceFormatter.FormatPrice(orderItem.PriceInclTax, _orderCurrency);
                if (orderItem.DiscountAmountInclTax > 0)
                    orderItemModel.Discount =
                        _priceFormatter.FormatPrice(orderItem.DiscountAmountInclTax, _orderCurrency);
            }
            else
            {
                //excluding tax
                orderItemModel.UnitPrice = _priceFormatter.FormatPrice(orderItem.UnitPriceExclTax, _orderCurrency);
                orderItemModel.UnitPriceValue = orderItem.UnitPriceExclTax;

                orderItemModel.UnitPriceWithoutDiscount =
                    _priceFormatter.FormatPrice(orderItem.UnitPriceWithoutDiscExclTax, _orderCurrency);
                orderItemModel.UnitPriceWithoutDiscountValue = orderItem.UnitPriceWithoutDiscExclTax;

                orderItemModel.SubTotal = _priceFormatter.FormatPrice(orderItem.PriceExclTax, _orderCurrency);
                if (orderItem.DiscountAmountExclTax > 0)
                    orderItemModel.Discount =
                        _priceFormatter.FormatPrice(orderItem.DiscountAmountExclTax, _orderCurrency);
            }

            //downloadable products
            if (request.Order.IsDownloadAllowed(orderItem, product))
                orderItemModel.DownloadId = product.DownloadId;
            if (request.Order.IsLicenseDownloadAllowed(orderItem, product))
                orderItemModel.LicenseId = !string.IsNullOrEmpty(orderItem.LicenseDownloadId)
                    ? orderItem.LicenseDownloadId
                    : "";

            model.Items.Add(orderItemModel);
        }
    }

    private async Task<PictureModel> PrepareOrderItemPicture(Product product, IList<CustomAttribute> attributes,
        string productName)
    {
        var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService);
        return new PictureModel {
            Id = sciPicture?.Id,
            ImageUrl = await _pictureService.GetPictureUrl(sciPicture, 80),
            Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), productName),
            AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"),
                productName)
        };
    }
}