using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallOrderStatus()
        {
            var statuses = new List<OrderStatus>
            {
                new OrderStatus
                {
                    IsSystem = true,
                    StatusId = 10,
                    Name = "Pending",
                    DisplayOrder = 0,
                },
                new OrderStatus
                {
                    IsSystem = true,
                    StatusId = 20,
                    Name = "Processing",
                    DisplayOrder = 1,
                },
                new OrderStatus
                {
                    IsSystem = true,
                    StatusId = 30,
                    Name = "Complete",
                    DisplayOrder = 2,
                },
                new OrderStatus
                {
                    IsSystem = true,
                    StatusId = 40,
                    Name = "Cancelled",
                    DisplayOrder = 3,
                },
            };
            await _orderStatusRepository.InsertManyAsync(statuses);
        }
    }
}
