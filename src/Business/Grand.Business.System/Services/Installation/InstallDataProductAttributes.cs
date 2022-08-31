using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Catalog;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallProductAttributes()
        {
            var productAttributes = new List<ProductAttribute>
            {
                new ProductAttribute
                {
                    Name = "Color",
                    SeName = "color"
                },
                new ProductAttribute
                {
                    Name = "Custom Text",
                    SeName = "custom-text"
                },
                new ProductAttribute
                {
                    Name = "HDD",
                    SeName = "hdd"
                },
                new ProductAttribute
                {
                    Name = "OS",
                    SeName = "os"
                },
                new ProductAttribute
                {
                    Name = "Processor",
                    SeName  = "processor"
                },
                new ProductAttribute
                {
                    Name = "RAM",
                    SeName = "ram"
                },
                new ProductAttribute
                {
                    Name = "Size",
                    SeName = "size"
                },
                new ProductAttribute
                {
                    Name = "Software",
                    SeName = "software"
                },
            };
            await _productAttributeRepository.InsertAsync(productAttributes);
        }
    }
}
