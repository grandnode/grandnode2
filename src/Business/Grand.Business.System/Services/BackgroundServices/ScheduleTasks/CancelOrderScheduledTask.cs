using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Common;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks;

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

    private DateTime CalculateStartCancelDate(int daysToCancelUnpaidOrder)
    {
        return DateTime.UtcNow.Date.AddDays(-daysToCancelUnpaidOrder);
    }
}