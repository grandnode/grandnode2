using Grand.Domain.Orders;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallOrderTags()
    {
        var giftTag = new OrderTag {
            Name = "gift",
            Count = 0
        };
        await _orderTagRepository.InsertAsync(giftTag);

        var priorityTag = new OrderTag {
            Name = "priority",
            Count = 0
        };
        await _orderTagRepository.InsertAsync(priorityTag);

        var wholesaleTag = new OrderTag {
            Name = "wholesale",
            Count = 0
        };
        await _orderTagRepository.InsertAsync(wholesaleTag);
    }
}
