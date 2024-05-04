using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallProductLayouts()
    {
        var productLayouts = new List<ProductLayout> {
            new() {
                Name = "Simple product",
                ViewPath = "ProductLayout.Simple",
                DisplayOrder = 10
            },
            new() {
                Name = "Grouped product (with variants)",
                ViewPath = "ProductLayout.Grouped",
                DisplayOrder = 100
            }
        };
        productLayouts.ForEach(x => _productLayoutRepository.Insert(x));
        return Task.CompletedTask;
    }
}