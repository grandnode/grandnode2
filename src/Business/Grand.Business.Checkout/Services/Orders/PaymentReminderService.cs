using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
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
    IMediator mediator,
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
            var overdue = await UnpaidOrderIds(store.Id, o =>
                o.PaymentDueDateUtc < today && o.PaymentOverdueNoticeSentOnUtc == null, cancellationToken);
            foreach (var orderId in overdue)
                await Notify(orderId, true, utcNow, cancellationToken);

            var orderSettings = await settingService.LoadSetting<OrderSettings>(store.Id);
            //bounded as in the settings validator: a value stored past it must not stop the run for every store
            var daysBefore = Math.Clamp(orderSettings.PaymentReminderDaysBefore, 0,
                OrderSettings.MaxPaymentReminderDaysBefore);
            var lastDueDate = today.AddDays(daysBefore);
            var dueSoon = await UnpaidOrderIds(store.Id, o =>
                o.PaymentDueDateUtc >= today && o.PaymentDueDateUtc <= lastDueDate &&
                o.PaymentReminderSentOnUtc == null, cancellationToken);
            foreach (var orderId in dueSoon)
                await Notify(orderId, false, utcNow, cancellationToken);
        }
    }

    /// <summary>
    ///     Ids of the store's orders bought on credit and not paid yet: an existing shop may hold many of them, so each
    ///     order is loaded only when its e-mail is sent
    /// </summary>
    private Task<IList<string>> UnpaidOrderIds(string storeId, Expression<Func<Order, bool>> filter,
        CancellationToken cancellationToken)
    {
        return orderRepository.ToListAsync(orderRepository.Table
            .Where(o => o.StoreId == storeId)
            .Where(PaymentExtensions.UnpaidOnCredit)
            .Where(filter)
            .Select(o => o.Id), cancellationToken);
    }

    private async Task Notify(string orderId, bool overdue, DateTime utcNow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var order = await orderRepository.GetByIdAsync(orderId);
            if (order == null)
                return;

            var customer = await customerService.GetCustomerById(order.CustomerId);
            var queued = overdue
                ? await orderMessageProviderService.SendPaymentOverdueCustomerMessage(order, customer,
                    order.CustomerLanguageId)
                : await orderMessageProviderService.SendPaymentDueReminderCustomerMessage(order, customer,
                    order.CustomerLanguageId);

            //marked also when an inactive template, or one not meant for this store, queued nothing: switching it
            //on later does not mail every order that fell due meanwhile. A field update, not the whole order: a
            //payment saved meanwhile must not be overwritten
            if (overdue)
            {
                order.PaymentOverdueNoticeSentOnUtc = utcNow;
                await orderRepository.UpdateField(order.Id, x => x.PaymentOverdueNoticeSentOnUtc, utcNow);
            }
            else
            {
                order.PaymentReminderSentOnUtc = utcNow;
                await orderRepository.UpdateField(order.Id, x => x.PaymentReminderSentOnUtc, utcNow);
            }

            await mediator.EntityUpdated(order);

            if (queued > 0)
                await orderService.InsertOrderNote(new OrderNote {
                    Note = overdue ? "Payment overdue e-mail sent" : "Payment due reminder e-mail sent",
                    DisplayToCustomer = false,
                    OrderId = order.Id
                });
        }
        catch (Exception ex)
        {
            //not marked when the e-mail could not be queued: the next run tries again
            logger.LogError(ex, "Payment reminder for order {OrderId} failed", orderId);
        }
    }
}
