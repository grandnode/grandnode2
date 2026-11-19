using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Payment reminder service
/// </summary>
public class PaymentReminderService(
    IRepository<Order> orderRepository,
    IStoreService storeService,
    ISettingService settingService,
    ICustomerService customerService,
    IOrderService orderService,
    IOrderMessageProviderService orderMessageProviderService,
    ILogger<PaymentReminderService> logger) : IPaymentReminderService
{
    public async Task SendPaymentReminders(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        //the due date is a calendar day, as in PaymentExtensions.IsPaymentOverdue: due today is not overdue yet
        var today = utcNow.Date;
        foreach (var store in await storeService.GetAllStores())
        {
            cancellationToken.ThrowIfCancellationRequested();

            //the overdue notices first: an order that is overdue already is not reminded that it falls due
            var overdue = await orderRepository.ToListAsync(UnpaidOrders(store.Id).Where(o =>
                o.PaymentDueDateUtc < today && o.PaymentOverdueNoticeSentOnUtc == null), cancellationToken);
            foreach (var order in overdue)
                await Notify(order, x => x.PaymentOverdueNoticeSentOnUtc, "Payment overdue e-mail sent",
                    orderMessageProviderService.SendPaymentOverdueCustomerMessage, utcNow, cancellationToken);

            var orderSettings = await settingService.LoadSetting<OrderSettings>(store.Id);
            var lastDueDate = today.AddDays(Math.Max(0, orderSettings.PaymentReminderDaysBefore));
            var dueSoon = await orderRepository.ToListAsync(UnpaidOrders(store.Id).Where(o =>
                o.PaymentDueDateUtc >= today && o.PaymentDueDateUtc <= lastDueDate &&
                o.PaymentReminderSentOnUtc == null), cancellationToken);
            foreach (var order in dueSoon)
                await Notify(order, x => x.PaymentReminderSentOnUtc, "Payment due reminder e-mail sent",
                    orderMessageProviderService.SendPaymentDueReminderCustomerMessage, utcNow, cancellationToken);
        }
    }

    /// <summary>
    ///     Orders of the store bought on credit and not paid yet - the same rule as PaymentExtensions.IsPaymentOverdue
    /// </summary>
    private IQueryable<Order> UnpaidOrders(string storeId)
    {
        return orderRepository.Table.Where(o =>
            o.StoreId == storeId && o.PaymentDueDateUtc != null && !o.Deleted &&
            o.OrderStatusId != (int)OrderStatusSystem.Cancelled &&
            (o.PaymentStatusId == PaymentStatus.Pending ||
             o.PaymentStatusId == PaymentStatus.Authorized ||
             o.PaymentStatusId == PaymentStatus.PartiallyPaid));
    }

    private async Task Notify(Order order, Expression<Func<Order, DateTime?>> sentOn, string note,
        Func<Order, Customer, string, Task<int>> send, DateTime utcNow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var customer = await customerService.GetCustomerById(order.CustomerId);
            //an inactive or missing template queues nothing: the order is marked anyway, so switching the template
            //on later does not mail every order that fell due meanwhile
            if (await send(order, customer, order.CustomerLanguageId) > 0)
                await orderService.InsertOrderNote(new OrderNote {
                    Note = note,
                    DisplayToCustomer = false,
                    OrderId = order.Id
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment reminder for order {OrderId} failed", order.Id);
        }
        finally
        {
            //a field update, not the whole order: a payment saved meanwhile must not be overwritten. Marked after a
            //failure too, so one broken order is not retried and logged every day
            await orderRepository.UpdateField(order.Id, sentOn, utcNow);
        }
    }
}
