using Grand.Domain.Shipping;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallShippingMethods()
    {
        var shippingMethods = new List<ShippingMethod> {
            new() {
                Name = "Ground",
                Description = "Compared to other shipping methods, ground shipping is carried out closer to the earth",
                DisplayOrder = 1
            },
            new() {
                Name = "Next Day Air",
                Description = "The one day air shipping",
                DisplayOrder = 3
            },
            new() {
                Name = "2nd Day Air",
                Description = "The two day air shipping",
                DisplayOrder = 3
            }
        };
        shippingMethods.ForEach(x => _shippingMethodRepository.Insert(x));
        return Task.CompletedTask;
    }
}