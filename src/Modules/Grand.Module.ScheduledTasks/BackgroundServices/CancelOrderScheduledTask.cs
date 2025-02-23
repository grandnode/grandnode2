using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Common;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

public class CancelOrderScheduledTask : IScheduleTask
{
    private readonly IOrderService _orderService;
    private readonly SystemSettings _systemSettings;

    public CancelOrderScheduledTask(
        SystemSettings systemSettings,
        IOrderService orderService)
    {
        _systemSettings = systemSettings;
        _orderService = orderService;
    }

    public async Task Execute()
    {
        if (!_systemSettings.DaysToCancelUnpaidOrder.HasValue)
            return;

        var startCancelDate = CalculateStartCancelDate(_systemSettings.DaysToCancelUnpaidOrder.Value);
        await _orderService.CancelExpiredOrders(startCancelDate);
    }

    private static DateTime CalculateStartCancelDate(int daysToCancelUnpaidOrder) => DateTime.UtcNow.Date.AddDays(-daysToCancelUnpaidOrder);
}