using Grand.Domain.Shipping;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallDeliveryDates()
    {
        var deliveryDates = new List<DeliveryDate> {
            new() {
                Name = "1-2 days",
                DisplayOrder = 1
            },
            new() {
                Name = "3-5 days",
                DisplayOrder = 5
            },
            new() {
                Name = "1 week",
                DisplayOrder = 10
            }
        };
        deliveryDates.ForEach(x => _deliveryDateRepository.Insert(x));
        return Task.CompletedTask;
    }
}