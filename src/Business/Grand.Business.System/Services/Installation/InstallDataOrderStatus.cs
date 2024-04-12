using Grand.Domain.Orders;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallOrderStatus()
    {
        var statuses = new List<OrderStatus> {
            new() {
                IsSystem = true,
                StatusId = 10,
                Name = "Pending",
                DisplayOrder = 0
            },
            new() {
                IsSystem = true,
                StatusId = 20,
                Name = "Processing",
                DisplayOrder = 1
            },
            new() {
                IsSystem = true,
                StatusId = 30,
                Name = "Complete",
                DisplayOrder = 2
            },
            new() {
                IsSystem = true,
                StatusId = 40,
                Name = "Cancelled",
                DisplayOrder = 3
            }
        };
        statuses.ForEach(x => _orderStatusRepository.Insert(x));
        return Task.CompletedTask;
    }
}