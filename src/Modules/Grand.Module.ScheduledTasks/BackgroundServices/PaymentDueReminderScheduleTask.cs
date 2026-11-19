using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     E-mails the customers of orders bought on credit before the payment falls due and once it is overdue
/// </summary>
public class PaymentDueReminderScheduleTask(IPaymentReminderService paymentReminderService) : IScheduleTask
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        await paymentReminderService.SendPaymentReminders(DateTime.UtcNow, cancellationToken);
    }
}
