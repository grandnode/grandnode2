using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Common;

namespace Grand.Business.System.Services.BackgroundServices.ScheduleTasks
{
    public partial class CancelOrderScheduledTask : IScheduleTask
    {
        private readonly SystemSettings _systemSettings;
        private readonly IOrderService _orderService;

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

            DateTime startCancelDate = CalculateStartCancelDate(_systemSettings.DaysToCancelUnpaidOrder.Value);
            await _orderService.CancelExpiredOrders(startCancelDate);
        }

        private DateTime CalculateStartCancelDate(int daysToCancelUnpaidOrder)
        {
            return DateTime.UtcNow.Date.AddDays(-daysToCancelUnpaidOrder);
        }
    }
}
