using System.Globalization;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class PlaceRecurringCycleOrderCommandHandler(
    IOrderService orderService,
    IPaymentTransactionService paymentTransactionService,
    IProductService productService,
    IInventoryManageService inventoryManageService,
    ICustomerService customerService,
    IGiftVoucherService giftVoucherService,
    IMediator mediator,
    ILogger<PlaceRecurringCycleOrderCommandHandler> logger)
    : IRequestHandler<PlaceRecurringCycleOrderCommand, Order>
{
    public async Task<Order> Handle(PlaceRecurringCycleOrderCommand request, CancellationToken cancellationToken)
    {
        var recurringPayment = request.RecurringPayment;
        var cycle = request.Cycle;

        var initialOrder = await orderService.GetOrderById(recurringPayment.InitialOrderId);
        if (initialOrder == null || initialOrder.Deleted)
            throw new GrandException(
                $"The initial order {recurringPayment.InitialOrderId} of recurring payment {recurringPayment.Id} was not found");

        // a gift voucher paid part of the first cycle only
        var giftVouchersAmount = (await giftVoucherService.GetAllGiftVoucherUsageHistory(initialOrder.Id))
            .Sum(x => x.UsedValue);

        var code = await mediator.Send(new PrepareOrderCodeCommand(), cancellationToken);
        var order = RecurringCycleOrder.Copy(initialOrder, cycle.OrderId, cycle.CycleNumber, code, DateTime.UtcNow,
            giftVouchersAmount);

        // the gateway already took the money, so the transaction is a real one from the start
        var paymentTransaction = new PaymentTransaction {
            TransactionStatus = TransactionStatus.Pending,
            OrderGuid = order.OrderGuid,
            OrderCode = order.Code,
            StoreId = order.StoreId,
            CustomerId = recurringPayment.CustomerId,
            CustomerEmail = order.CustomerEmail,
            PaymentMethodSystemName = order.PaymentMethodSystemName,
            CurrencyCode = order.CustomerCurrencyCode,
            CurrencyRate = order.CurrencyRate,
            TransactionAmount = order.OrderTotal,
            Temp = false
        };
        await paymentTransactionService.InsertPaymentTransaction(paymentTransaction);

        await orderService.InsertOrder(order);

        // no stock check: the customer has paid, so low stock never blocks a cycle order
        foreach (var item in order.OrderItems)
        {
            var product = await productService.GetProductById(item.ProductId);
            if (product == null) continue;

            await inventoryManageService.AdjustReserved(product, -item.Quantity, item.Attributes, item.WarehouseId);

            // statistics only: the payment is already taken, so a failed write must not stop the order
            try
            {
                await productService.IncrementProductField(product, x => x.Sold, item.Quantity);
            }
            catch (Exception exc)
            {
                logger.LogWarning(exc, "Could not update the sold count of product {ProductId}", product.Id);
            }
        }

        await InsertNote(order.Id,
            $"Cycle {cycle.CycleNumber} of the recurring payment of order #{initialOrder.OrderNumber}");

        // no request context here: the customer comes from the order, and no host falls back to the store's primary one
        var customer = await customerService.GetCustomerById(order.CustomerId);
        await mediator.Send(new OrderNotificationCommand { Order = order, Customer = customer, Host = null },
            cancellationToken);

        await mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

        await mediator.Publish(new OrderPlacedEvent(order), cancellationToken);

        // the order is paid with what the gateway charged: a smaller charge must not show as fully paid
        var charged = Math.Round(cycle.Amount, 2);
        var total = Math.Round(order.OrderTotal, 2);
        if (charged >= total)
            await mediator.Send(new MarkAsPaidCommand {
                PaymentTransaction = paymentTransaction,
                Amount = cycle.Amount,
                Reference = cycle.ProviderReference,
                CreatedBy = recurringPayment.PaymentMethodSystemName
            }, cancellationToken);
        else if (charged > 0)
            await mediator.Send(new PartiallyPaidOfflineCommand {
                PaymentTransaction = paymentTransaction,
                AmountToPaid = cycle.Amount,
                Reference = cycle.ProviderReference,
                CreatedBy = recurringPayment.PaymentMethodSystemName
            }, cancellationToken);

        // a price change at the gateway shows here
        if (charged != total)
            await InsertNote(order.Id,
                $"The payment gateway charged {Format(charged, order)} for this cycle; the order total is {Format(total, order)}");

        return order;
    }

    private static string Format(double amount, Order order)
    {
        return $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {order.CustomerCurrencyCode}";
    }

    private Task InsertNote(string orderId, string note)
    {
        return orderService.InsertOrderNote(new OrderNote {
            Note = note,
            DisplayToCustomer = false,
            OrderId = orderId
        });
    }
}
