using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Media;
using Grand.Web.Models.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Orders
{
    public class GetOrderDetailsHandler : IRequestHandler<GetOrderDetails, OrderDetailsModel>
    {
        private readonly IDateTimeService _dateTimeService;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ITranslationService _translationService;
        private readonly IShipmentService _shipmentService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly IOrderService _orderService;
        private readonly IPictureService _pictureService;
        private readonly IDownloadService _downloadService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly IMediator _mediator;
        private readonly CatalogSettings _catalogSettings;
        private readonly OrderSettings _orderSettings;
        private readonly PdfSettings _pdfSettings;
        private readonly TaxSettings _taxSettings;

        public GetOrderDetailsHandler(
            IDateTimeService dateTimeService,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            ITranslationService translationService,
            IShipmentService shipmentService,
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService,
            IPriceFormatter priceFormatter,
            IGiftVoucherService giftVoucherService,
            IOrderService orderService,
            IPictureService pictureService,
            IDownloadService downloadService,
            IOrderStatusService orderStatusService,
            IMediator mediator,
            CatalogSettings catalogSettings,
            OrderSettings orderSettings,
            PdfSettings pdfSettings,
            TaxSettings taxSettings)
        {
            _dateTimeService = dateTimeService;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _translationService = translationService;
            _shipmentService = shipmentService;
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
            _priceFormatter = priceFormatter;
            _giftVoucherService = giftVoucherService;
            _orderService = orderService;
            _pictureService = pictureService;
            _downloadService = downloadService;
            _orderStatusService = orderStatusService;
            _mediator = mediator;
            _orderSettings = orderSettings;
            _catalogSettings = catalogSettings;
            _pdfSettings = pdfSettings;
            _taxSettings = taxSettings;
        }

        public async Task<OrderDetailsModel> Handle(GetOrderDetails request, CancellationToken cancellationToken)
        {
            var model = new OrderDetailsModel();

            model.Id = request.Order.Id;
            model.OrderNumber = request.Order.OrderNumber;
            model.OrderCode = request.Order.Code;
            model.CreatedOn = _dateTimeService.ConvertToUserTime(request.Order.CreatedOnUtc, DateTimeKind.Utc);
            model.OrderStatus = (await _orderStatusService.GetByStatusId(request.Order.OrderStatusId))?.Name;
            model.IsReOrderAllowed = _orderSettings.IsReOrderAllowed;
            model.IsMerchandiseReturnAllowed = await _mediator.Send(new IsMerchandiseReturnAllowedQuery() { Order = request.Order });
            model.PdfInvoiceDisabled = _pdfSettings.DisablePdfInvoicesForPendingOrders && request.Order.OrderStatusId == (int)OrderStatusSystem.Pending;
            model.ShowAddOrderNote = _orderSettings.AllowCustomerToAddOrderNote;

            //shipping info
            await PrepareShippingInfo(request, model);

            //billing info
            model.BillingAddress = await _mediator.Send(new GetAddressModel()
            {
                Language = request.Language,
                Model = null,
                Address = request.Order.BillingAddress,
                ExcludeProperties = false,
            });

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
            {
                if (request.Order.OrderStatusId == (int)OrderStatusSystem.Pending && request.Order.PaymentStatusId == Domain.Payments.PaymentStatus.Pending
                    && (request.Order.ShippingStatusId == ShippingStatus.ShippingNotRequired || request.Order.ShippingStatusId == ShippingStatus.Pending))
                    model.UserCanCancelUnpaidOrder = true;
            }

            //purchased products
            await PrepareOrderItems(request, model);
            return model;

        }

        private async Task PrepareShippingInfo(GetOrderDetails request, OrderDetailsModel model)
        {
            model.ShippingStatus = request.Order.ShippingStatusId.GetTranslationEnum(_translationService, request.Language.Id);
            if (request.Order.ShippingStatusId != ShippingStatus.ShippingNotRequired)
            {
                model.IsShippable = true;
                model.PickUpInStore = request.Order.PickUpInStore;
                if (!request.Order.PickUpInStore)
                {
                    model.ShippingAddress = await _mediator.Send(new GetAddressModel()
                    {
                        Language = request.Language,
                        Model = null,
                        Address = request.Order.ShippingAddress,
                        ExcludeProperties = false,
                    });
                }
                else
                {
                    if (request.Order.PickupPoint != null)
                    {
                        if (request.Order.PickupPoint.Address != null)
                        {
                            model.PickupAddress = await _mediator.Send(new GetAddressModel()
                            {
                                Language = request.Language,
                                Address = request.Order.PickupPoint.Address,
                                ExcludeProperties = false,
                            });
                        }
                    }
                }
                model.ShippingMethod = request.Order.ShippingMethod;
                model.ShippingAdditionDescription = request.Order.ShippingOptionAttributeDescription;
                //shipments (only already shipped)
                var shipments = (await _shipmentService.GetShipmentsByOrder(request.Order.Id)).Where(x => x.ShippedDateUtc.HasValue).OrderBy(x => x.CreatedOnUtc).ToList();
                foreach (var shipment in shipments)
                {
                    var shipmentModel = new OrderDetailsModel.ShipmentBriefModel
                    {
                        Id = shipment.Id,
                        ShipmentNumber = shipment.ShipmentNumber,
                        TrackingNumber = shipment.TrackingNumber,
                    };
                    if (shipment.ShippedDateUtc.HasValue)
                        shipmentModel.ShippedDate = _dateTimeService.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                    if (shipment.DeliveryDateUtc.HasValue)
                        shipmentModel.DeliveryDate = _dateTimeService.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
                    model.Shipments.Add(shipmentModel);
                }
            }

        }

        private async Task PreparePaymentMethod(GetOrderDetails request, OrderDetailsModel model)
        {
            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName);
            model.PaymentMethod = paymentMethod != null ? paymentMethod.FriendlyName : request.Order.PaymentMethodSystemName;
            model.PaymentMethodStatus = request.Order.PaymentStatusId.GetTranslationEnum(_translationService, request.Language.Id);
            var paymentTransaction = await _paymentTransactionService.GetByOrdeGuid(request.Order.OrderGuid);
            model.CanRePostProcessPayment = paymentTransaction != null ? await _paymentService.CanRePostRedirectPayment(paymentTransaction) : false;
        }

        private async Task PrepareOrderTotal(GetOrderDetails request, OrderDetailsModel model)
        {
            if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
            {
                //including tax

                //order subtotal
                model.OrderSubtotal = await _priceFormatter.FormatPrice(request.Order.OrderSubtotalInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                //discount (applied to order subtotal)
                if (request.Order.OrderSubTotalDiscountInclTax > 0)
                    model.OrderSubTotalDiscount = await _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
            }
            else
            {
                //excluding tax

                //order subtotal
                model.OrderSubtotal = await _priceFormatter.FormatPrice(request.Order.OrderSubtotalExclTax, request.Order.CustomerCurrencyCode, request.Language, false);
                //discount (applied to order subtotal)
                if (request.Order.OrderSubTotalDiscountExclTax > 0)
                    model.OrderSubTotalDiscount = await _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountExclTax, request.Order.CustomerCurrencyCode, request.Language, false);
            }

            if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
            {
                //including tax

                //order shipping
                model.OrderShipping = await _priceFormatter.FormatShippingPrice(request.Order.OrderShippingInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                //payment method additional fee
                if (request.Order.PaymentMethodAdditionalFeeInclTax > 0)
                    model.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFee(request.Order.PaymentMethodAdditionalFeeInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
            }
            else
            {
                //excluding tax

                //order shipping
                model.OrderShipping = await _priceFormatter.FormatShippingPrice(request.Order.OrderShippingExclTax, request.Order.CustomerCurrencyCode, request.Language, false);
                //payment method additional fee
                if (request.Order.PaymentMethodAdditionalFeeExclTax > 0)
                    model.PaymentMethodAdditionalFee = await _priceFormatter.FormatPaymentMethodAdditionalFee(request.Order.PaymentMethodAdditionalFeeExclTax, request.Order.CustomerCurrencyCode, request.Language, false);
            }

            //total
            model.OrderTotal = await _priceFormatter.FormatPrice(request.Order.OrderTotal, request.Order.CustomerCurrencyCode, false, request.Language);

        }

        private async Task PrepareTax(GetOrderDetails request, OrderDetailsModel model)
        {
            bool displayTax = true;
            bool displayTaxRates = true;
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

                    model.Tax = await _priceFormatter.FormatPrice(request.Order.OrderTax, request.Order.CustomerCurrencyCode, false, request.Language);

                    foreach (var tr in request.Order.OrderTaxes)
                    {
                        model.TaxRates.Add(new OrderDetailsModel.TaxRate
                        {
                            Rate = _priceFormatter.FormatTaxRate(tr.Percent),
                            Value = await _priceFormatter.FormatPrice(tr.Amount, request.Order.CustomerCurrencyCode, false, request.Language),
                        });
                    }
                }
            }
            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;
            model.PricesIncludeTax = request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;

        }

        private async Task PrepareDiscount(GetOrderDetails request, OrderDetailsModel model)
        {
            if (request.Order.OrderDiscount > 0)
                model.OrderTotalDiscount = await _priceFormatter.FormatPrice(-request.Order.OrderDiscount, request.Order.CustomerCurrencyCode, false, request.Language);
        }

        private async Task PrepareGiftVouchers(GetOrderDetails request, OrderDetailsModel model)
        {
            foreach (var gcuh in await _giftVoucherService.GetAllGiftVoucherUsageHistory(request.Order.Id))
            {
                var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
                model.GiftVouchers.Add(new OrderDetailsModel.GiftVoucher
                {
                    CouponCode = giftVoucher.Code,
                    Amount = await _priceFormatter.FormatPrice(-gcuh.UsedValue, request.Order.CustomerCurrencyCode, false, request.Language),
                });
            }

        }

        private async Task PrepareLoyaltyPoints(GetOrderDetails request, OrderDetailsModel model)
        {
            if (request.Order.RedeemedLoyaltyPoints > 0)
            {
                model.RedeemedLoyaltyPoints = request.Order.RedeemedLoyaltyPoints;
                model.RedeemedLoyaltyPointsAmount = await _priceFormatter.FormatPrice(-request.Order.RedeemedLoyaltyPointsAmount, request.Order.CustomerCurrencyCode, false, request.Language);
            }

        }

        private async Task PrepareOrderNotes(GetOrderDetails request, OrderDetailsModel model)
        {
            foreach (var orderNote in (await _orderService.GetOrderNotes(request.Order.Id))
                .Where(on => on.DisplayToCustomer)
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList())
            {
                model.OrderNotes.Add(new OrderDetailsModel.OrderNote
                {
                    Id = orderNote.Id,
                    OrderId = orderNote.OrderId,
                    HasDownload = !string.IsNullOrEmpty(orderNote.DownloadId),
                    Note = orderNote.Note,
                    CreatedOn = _dateTimeService.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }

        }

        private async Task PrepareOrderItems(GetOrderDetails request, OrderDetailsModel model)
        {
            model.ShowSku = _catalogSettings.ShowSkuOnProductDetailsPage;
            foreach (var orderItem in request.Order.OrderItems)
            {
                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
                var orderItemModel = new OrderDetailsModel.OrderItemModel
                {
                    Id = orderItem.Id,
                    OrderItemGuid = orderItem.OrderItemGuid,
                    Sku = product.FormatSku(orderItem.Attributes, _productAttributeParser),
                    ProductId = product.Id,
                    ProductName = product.GetTranslation(x => x.Name, request.Language.Id),
                    ProductSeName = product.SeName,
                    Quantity = orderItem.Quantity,
                    AttributeInfo = orderItem.AttributeDescription,
                };
                //prepare picture
                orderItemModel.Picture = await PrepareOrderItemPicture(product, orderItem.Attributes, orderItemModel.ProductName);

                model.Items.Add(orderItemModel);

                //unit price, subtotal
                if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    orderItemModel.UnitPrice = await _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                    orderItemModel.UnitPriceValue = orderItem.UnitPriceInclTax;

                    orderItemModel.UnitPriceWithoutDiscount = await _priceFormatter.FormatPrice(orderItem.UnitPriceWithoutDiscInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                    orderItemModel.UnitPriceWithoutDiscountValue = orderItem.UnitPriceWithoutDiscInclTax;

                    orderItemModel.SubTotal = await _priceFormatter.FormatPrice(orderItem.PriceInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                    if (orderItem.DiscountAmountInclTax > 0)
                    {
                        orderItemModel.Discount = await _priceFormatter.FormatPrice(orderItem.DiscountAmountInclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                    }
                }
                else
                {
                    //excluding tax
                    orderItemModel.UnitPrice = await _priceFormatter.FormatPrice(orderItem.UnitPriceExclTax, request.Order.CustomerCurrencyCode, request.Language, false);
                    orderItemModel.UnitPriceValue = orderItem.UnitPriceExclTax;

                    orderItemModel.UnitPriceWithoutDiscount = await _priceFormatter.FormatPrice(orderItem.UnitPriceWithoutDiscExclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                    orderItemModel.UnitPriceWithoutDiscountValue = orderItem.UnitPriceWithoutDiscExclTax;

                    orderItemModel.SubTotal = await _priceFormatter.FormatPrice(orderItem.PriceExclTax, request.Order.CustomerCurrencyCode, request.Language, false);
                    if (orderItem.DiscountAmountExclTax > 0)
                    {
                        orderItemModel.Discount = await _priceFormatter.FormatPrice(orderItem.DiscountAmountExclTax, request.Order.CustomerCurrencyCode, request.Language, true);
                    }
                }

                //downloadable products
                if (request.Order.IsDownloadAllowed(orderItem, product))
                    orderItemModel.DownloadId = product.DownloadId;
                if (request.Order.IsLicenseDownloadAllowed(orderItem, product))
                    orderItemModel.LicenseId = !string.IsNullOrEmpty(orderItem.LicenseDownloadId) ? orderItem.LicenseDownloadId : "";
            }

        }

        private async Task<PictureModel> PrepareOrderItemPicture(Product product, IList<CustomAttribute> attributes, string productName)
        {
            var sciPicture = await product.GetProductPicture(attributes, _productService, _pictureService, _productAttributeParser);
            return new PictureModel
            {
                Id = sciPicture?.Id,
                ImageUrl = await _pictureService.GetPictureUrl(sciPicture, 80),
                Title = string.Format(_translationService.GetResource("Media.Product.ImageLinkTitleFormat"), productName),
                AlternateText = string.Format(_translationService.GetResource("Media.Product.ImageAlternateTextFormat"), productName),
            };
        }
    }
}
