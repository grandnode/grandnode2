namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     E-mails the customers of orders bought on credit: once before the payment falls due, once when it is overdue
/// </summary>
public interface IPaymentReminderService
{
    /// <summary>
    ///     Sends the reminders and overdue notices not sent yet. Each order gets each e-mail once, until its due date
    ///     changes; an order already overdue gets only the overdue notice
    /// </summary>
    /// <param name="utcNow">Current date and time in UTC</param>
    /// <param name="cancellationToken">Cancellation token, checked between orders</param>
    Task SendPaymentReminders(DateTime utcNow, CancellationToken cancellationToken = default);
}
