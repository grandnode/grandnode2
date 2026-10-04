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
            },
            new() {
                Name = "Processor",
                SeName = "processor"
            },
            new() {
                Name = "Memory",
                SeName = "memory"
            },
            new() {
                Name = "Storage",
                SeName = "storage"
            },
            new() {
                Name = "Graphics",
                SeName = "graphics"
            },
            new() {
                Name = "Operating system",
                SeName = "operating-system"
            },
            new() {
                Name = "Software",
                SeName = "software"
            }
        };
        productAttributes.ForEach(x => _productAttributeRepository.Insert(x));
        return Task.CompletedTask;
    }
}