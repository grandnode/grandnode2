using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual Task InstallProductAttributes()
    {
        var productAttributes = new List<ProductAttribute> {
            new() {
                Name = "Color",
                SeName = "color"
            },
            new() {
                Name = "Size",
                SeName = "size"
            },
            new() {
                Name = "Material",
                SeName = "material"
            },
            new() {
                Name = "Capacity",
                SeName = "capacity"
            },
            new() {
                Name = "Engraving",
                SeName = "engraving"
            },
            new() {
                Name = "Grind",
                SeName = "grind"
            },
            new() {
                Name = "Rental add-ons",
                SeName = "rental-add-ons"
            }
        };
        productAttributes.ForEach(x => _productAttributeRepository.Insert(x));
        return Task.CompletedTask;
    }
}