using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Commands.Handlers.Orders
{
    public class OrderStatusCommandHandler : IRequestHandler<SetOrderStatusCommand, bool>
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IPdfService _pdfService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IVendorService _vendorService;
        private readonly IMediator _mediator;
        private readonly OrderSettings _orderSettings;
        private readonly LoyaltyPointsSettings _loyaltyPointsSettings;

        public OrderStatusCommandHandler(
            IOrderService orderService,
            ICustomerService customerService,
            IPdfService pdfService,
            IMessageProviderService messageProviderService,
            IVendorService vendorService,
            IMediator mediator,
            OrderSettings orderSettings,
            LoyaltyPointsSettings loyaltyPointsSettings)
        {
            _orderService = orderService;
            _customerService = customerService;
            _pdfService = pdfService;
            _messageProviderService = messageProviderService;
            _vendorService = vendorService;
            _mediator = mediator;
            _orderSettings = orderSettings;
            _loyaltyPointsSettings = loyaltyPointsSettings;
        }

        public async Task<bool> Handle(SetOrderStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.Order == null)
                throw new ArgumentNullException(nameof(request.Order));

            int prevOrderStatus = request.Order.OrderStatusId;
            if (prevOrderStatus == (int)request.Os)
                return false;

            //set and save new order status
            request.Order.OrderStatusId = (int)request.Os;
            await _orderService.UpdateOrder(request.Order);

            //order notes, notifications
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = string.Format("Order status has been changed to {0}", request.Os.ToString()),
                DisplayToCustomer = false,
                OrderId = request.Order.Id,
                CreatedOnUtc = DateTime.UtcNow
            });

            var customer = await _customerService.GetCustomerById(request.Order.CustomerId);

            if (prevOrderStatus != (int)OrderStatusSystem.Complete &&
                request.Os == OrderStatusSystem.Complete
                && request.NotifyCustomer)
            {
                //notification

                var orderCompletedAttachmentFilePath = _orderSettings.AttachPdfInvoiceToOrderCompletedEmail ?
                    await _pdfService.PrintOrderToPdf(request.Order, "") : null;
                var orderCompletedAttachmentFileName = _orderSettings.AttachPdfInvoiceToOrderCompletedEmail ?
                    "order.pdf" : null;

                var orderCompletedAttachments = _orderSettings.AttachPdfInvoiceToOrderCompletedEmail && _orderSettings.AttachPdfInvoiceToBinary ?
                    new List<string> { await _pdfService.SaveOrderToBinary(request.Order, "") } : new List<string>();

                int orderCompletedCustomerNotificationQueuedEmailId = await _messageProviderService
                    .SendOrderCompletedCustomerMessage(request.Order, customer, request.Order.CustomerLanguageId, orderCompletedAttachmentFilePath,
                    orderCompletedAttachmentFileName, orderCompletedAttachments);
                if (orderCompletedCustomerNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote
                    {
                        Note = "\"Order completed\" email (to customer) has been queued.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = request.Order.Id,
                    });
                }
            }

            if (prevOrderStatus != (int)OrderStatusSystem.Cancelled &&
                request.Os == OrderStatusSystem.Cancelled
                && request.NotifyCustomer)
            {
                //notification customer
                int orderCancelledCustomerNotificationQueuedEmailId = await _messageProviderService.SendOrderCancelledCustomerMessage(request.Order, customer, request.Order.CustomerLanguageId);
                if (orderCancelledCustomerNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote
                    {
                        Note = "\"Order cancelled\" email (to customer) has been queued.",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = request.Order.Id,
                    });
                }
                //notification for vendor
                await VendorNotification(request.Order);
            }

            if (prevOrderStatus != (int)OrderStatusSystem.Cancelled &&
                request.Os == OrderStatusSystem.Cancelled
                && request.NotifyStoreOwner)
            {
                //notification store owner
                var orderCancelledStoreOwnerNotificationQueuedEmailId = await _messageProviderService.SendOrderCancelledStoreOwnerMessage(request.Order, customer, request.Order.CustomerLanguageId);
                if (orderCancelledStoreOwnerNotificationQueuedEmailId > 0)
                {
                    await _orderService.InsertOrderNote(new OrderNote
                    {
                        Note = "\"Order cancelled\" by customer.",
                        DisplayToCustomer = true,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = request.Order.Id,
                    });
                }
                //notification for vendor
                await VendorNotification(request.Order);
            }

            //loyalty points
            if (_loyaltyPointsSettings.PointsForPurchases_Awarded == request.Order.OrderStatusId)
            {
                await _mediator.Send(new AwardLoyaltyPointsCommand() { Order = request.Order });
            }
            if (_loyaltyPointsSettings.ReduceLoyaltyPointsAfterCancelOrder && request.Order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
            {
                await _mediator.Send(new ReduceLoyaltyPointsCommand() { Order = request.Order });
            }

            //gift vouchers activation
            if (_orderSettings.GiftVouchers_Activated_OrderStatusId > 0 &&
               _orderSettings.GiftVouchers_Activated_OrderStatusId == (int)request.Order.OrderStatusId)
            {
                await _mediator.Send(new ActivatedValueForPurchasedGiftVouchersCommand() { Order = request.Order, Activate = true });
            }

            //gift vouchers deactivation
            if (_orderSettings.DeactivateGiftVouchersAfterCancelOrder &&
                request.Order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
            {
                await _mediator.Send(new ActivatedValueForPurchasedGiftVouchersCommand() { Order = request.Order, Activate = false });
            }

            return true;
        }

        private async Task<bool> VendorNotification(Order order)
        {
            //notification for vendor
            foreach (var orderItem in order.OrderItems)
            {
                if (!string.IsNullOrEmpty(orderItem.VendorId))
                {
                    var vendor = await _vendorService.GetVendorById(orderItem.VendorId);
                    if (vendor != null && !vendor.Deleted && vendor.Active)
                    {
                        await _messageProviderService.SendOrderCancelledVendorMessage(order, vendor, order.CustomerLanguageId);
                    }
                }
            }

            return true;
        }
    }
}
