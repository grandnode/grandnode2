using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation;

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
                Name = "Custom Text",
                SeName = "custom-text"
            },
            new() {
                Name = "HDD",
                SeName = "hdd"
            },
            new() {
                Name = "OS",
                SeName = "os"
            },
            new() {
                Name = "Processor",
                SeName = "processor"
            },
            new() {
                Name = "RAM",
                SeName = "ram"
            },
            new() {
                Name = "Size",
                SeName = "size"
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