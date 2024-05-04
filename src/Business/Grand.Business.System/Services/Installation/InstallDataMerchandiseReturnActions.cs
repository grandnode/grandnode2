using Grand.Domain.Orders;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallMerchandiseReturnActions()
    {
        var merchandiseReturnActions = new List<MerchandiseReturnAction> {
            new() {
                Name = "Repair",
                DisplayOrder = 1
            },
            new() {
                Name = "Replacement",
                DisplayOrder = 2
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