using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using System.Linq.Expressions;

namespace Grand.Business.Core.Extensions;

/// <summary>
///     Payment extensions
/// </summary>
public static class PaymentExtensions
{
    /// <summary>
    ///     Orders bought on credit and not paid yet: not deleted, not cancelled and with the payment still open. A query
    ///     filter, so the reminders, the company credit and the overdue order list all count the same orders
    /// </summary>
    public static readonly Expression<Func<Order, bool>> UnpaidOnCredit = o =>
        o.PaymentDueDateUtc != null &&
        !o.Deleted &&
        o.OrderStatusId != (int)OrderStatusSystem.Cancelled &&
        (o.PaymentStatusId == PaymentStatus.Pending ||
         o.PaymentStatusId == PaymentStatus.Authorized ||
         o.PaymentStatusId == PaymentStatus.PartiallyPaid);

    private static readonly Func<Order, bool> IsUnpaidOnCredit = UnpaidOnCredit.Compile();

    /// <summary>
    ///     Is payment method active?
    /// </summary>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="paymentSettings">Payment settings</param>
    /// <returns>Result</returns>
    public static bool IsPaymentMethodActive(this IPaymentProvider paymentMethod,
        PaymentSettings paymentSettings)
    {
        ArgumentNullException.ThrowIfNull(paymentMethod);
        ArgumentNullException.ThrowIfNull(paymentSettings);

        return paymentSettings.ActivePaymentProviderSystemNames != null &&
               paymentSettings.ActivePaymentProviderSystemNames.Any(activeMethodSystemName =>
                   paymentMethod.SystemName.Equals(activeMethodSystemName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets the name of the order's payment method in the order's language, for text that does not follow
    ///     the current request (e-mails, PDF). Falls back to the friendly name when the order has no language,
    ///     and to the system name when the provider is missing or gives no name.
    /// </summary>
    /// <param name="paymentMethod">Payment method, or null when it is no longer installed</param>
    /// <param name="order">Order</param>
    /// <returns>Payment method name</returns>
    public static string? GetPaymentMethodName(this IPaymentProvider? paymentMethod, Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (paymentMethod == null)
            return order.PaymentMethodSystemName;

        var name = string.IsNullOrEmpty(order.CustomerLanguageId)
            ? paymentMethod.FriendlyName
            : paymentMethod.GetFriendlyName(order.CustomerLanguageId);

        if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(order.CustomerLanguageId))
            name = paymentMethod.FriendlyName;

        return string.IsNullOrEmpty(name) ? order.PaymentMethodSystemName : name;
    }

    /// <summary>
    ///     Gets a value indicating whether the payment of an order bought on credit is overdue: the order is not
    ///     cancelled, not paid yet and its due date has passed. The due date is a calendar day (UTC), so the
    ///     payment becomes overdue the day after it.
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="utcNow">Current date and time in UTC</param>
    /// <returns>Result</returns>
    public static bool IsPaymentOverdue(this Order order, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(order);

        return IsUnpaidOnCredit(order) && order.PaymentDueDateUtc!.Value < utcNow.Date;
    }

    /// <summary>
    ///     Filters orders to those whose payment is overdue, by the same rule as <see cref="IsPaymentOverdue" />
    /// </summary>
    /// <param name="orders">Orders</param>
    /// <param name="utcNow">Current date and time in UTC</param>
    /// <returns>Orders bought on credit, not paid yet and past their due date</returns>
    public static IQueryable<Order> WherePaymentOverdue(this IQueryable<Order> orders, DateTime utcNow)
    {
        var today = utcNow.Date;
        return orders.Where(UnpaidOnCredit).Where(o => o.PaymentDueDateUtc < today);
    }

    /// <summary>
    ///     Filters orders to those carrying a purchase order number, matched exactly once trimmed
    /// </summary>
    /// <param name="orders">Orders</param>
    /// <param name="purchaseOrderNumber">Purchase order number</param>
    /// <returns>Orders carrying the number</returns>
    public static IQueryable<Order> WherePurchaseOrderNumber(this IQueryable<Order> orders,
        string purchaseOrderNumber)
    {
        var number = purchaseOrderNumber.Trim();
        return orders.Where(o => o.PurchaseOrderNumber == number);
    }

    /// <summary>
    ///     Gets what is left to pay of a payment transaction
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    /// <returns>The amount, never below zero</returns>
    public static double AmountLeftToPay(this PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        return AmountLeft(paymentTransaction.TransactionAmount, paymentTransaction.PaidAmount);
    }

    /// <summary>
    ///     Gets what is left to refund of a payment transaction
    /// </summary>
    /// <param name="paymentTransaction">Payment transaction</param>
    /// <returns>The amount, never below zero</returns>
    public static double AmountLeftToRefund(this PaymentTransaction paymentTransaction)
    {
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        return AmountLeft(paymentTransaction.TransactionAmount, paymentTransaction.RefundedAmount);
    }

    /// <summary>
    ///     Amounts paid or refunded in parts add up with floating-point residue (0.1 + 0.2 is not 0.3), which is
    ///     rounded off so that a transaction settled in full has nothing left. No currency has six decimals.
    /// </summary>
    private static double AmountLeft(double amount, double used) => Math.Max(Math.Round(amount - used, 6), 0);
}