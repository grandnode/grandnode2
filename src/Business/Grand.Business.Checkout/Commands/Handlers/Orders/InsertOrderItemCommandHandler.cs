using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class InsertOrderItemCommandHandler : IRequestHandler<InsertOrderItemCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly IInventoryManageService _inventoryManageService;

        public InsertOrderItemCommandHandler(
            IMediator mediator,
            IOrderService orderService,
            IGiftVoucherService giftVoucherService,
            IInventoryManageService inventoryManageService)
        {
            _mediator = mediator;
            _orderService = orderService;
            _giftVoucherService = giftVoucherService;
            _inventoryManageService = inventoryManageService;
        }

        public async Task<bool> Handle(InsertOrderItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            if (request.OrderItem == null)
                throw new ArgumentNullException(nameof(request.OrderItem));

            request.Order.OrderItems.Add(request.OrderItem);
            request.Order.OrderSubtotalExclTax += request.OrderItem.PriceExclTax;
            request.Order.OrderSubtotalInclTax += request.OrderItem.PriceInclTax;
            request.Order.OrderTax += (request.OrderItem.PriceInclTax - request.OrderItem.PriceExclTax);
            request.Order.OrderTotal += request.OrderItem.PriceInclTax;

            if (request.Order.ShippingStatusId == ShippingStatus.Delivered
                && request.OrderItem.IsShipEnabled)
            {
                request.Order.ShippingStatusId = ShippingStatus.PartiallyShipped;
            }

            if (request.Order.ShippingStatusId == ShippingStatus.ShippingNotRequired
                && request.OrderItem.IsShipEnabled)
            {
                request.Order.ShippingStatusId = ShippingStatus.Pending;
            }

            //TODO 
            //request.Order.OrderTaxes

            await _orderService.UpdateOrder(request.Order);

            //adjust inventory
            await _inventoryManageService.AdjustReserved(request.Product, -request.OrderItem.Quantity, request.OrderItem.Attributes, request.OrderItem.WarehouseId);

            //check order status
            await _mediator.Send(new CheckOrderStatusCommand() { Order = request.Order });

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "A new order item has been added",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = request.Order.Id,
            });

            //gift vouchers
            if (request.Product.IsGiftVoucher)
            {
                for (int i = 0; i < request.OrderItem.Quantity; i++)
                {
                    var gc = new GiftVoucher
                    {
                        GiftVoucherTypeId = request.Product.GiftVoucherTypeId,
                        PurchasedWithOrderItem = request.OrderItem,
                        Amount = request.OrderItem.UnitPriceExclTax,
                        CurrencyCode = request.Order.CustomerCurrencyCode,
                        IsGiftVoucherActivated = false,
                        Code = _giftVoucherService.GenerateGiftVoucherCode(),

                        //TODO
                        //RecipientName = recipientName,
                        //RecipientEmail = recipientEmail,
                        //SenderName = senderName,
                        //SenderEmail = senderEmail,
                        //Message = giftVoucherMessage,
                        IsRecipientNotified = false,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    await _giftVoucherService.InsertGiftVoucher(gc);
                }
            }
            return true;
        }
    }
}
