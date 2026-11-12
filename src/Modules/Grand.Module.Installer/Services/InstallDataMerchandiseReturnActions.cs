using Grand.Domain.Orders;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual Task InstallMerchandiseReturnActions()
    {
        var merchandiseReturnActions = new List<MerchandiseReturnAction> {
            new() {
                Name = "Repair",
                DisplayOrder = 1,
                WithoutRefund = true
            },
            new() {
                Name = "Replacement",
                DisplayOrder = 2,
                WithoutRefund = true
            },
            new() {
                Name = "Store Credit",
                DisplayOrder = 3
            }
        };
        merchandiseReturnActions.ForEach(x => _merchandiseReturnActionRepository.Insert(x));
        return Task.CompletedTask;
    }
}