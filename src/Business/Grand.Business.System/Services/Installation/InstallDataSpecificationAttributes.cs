using Grand.Business.Common.Extensions;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Catalog;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallSpecificationAttributes()
        {
            var sa1 = new SpecificationAttribute
            {
                Name = "Screensize",
                DisplayOrder = 1,
                SeName = SeoExtensions.GenerateSlug("Screensize", false, false, false),
            };
            await _specificationAttributeRepository.InsertAsync(sa1);

            sa1.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "13.0''",
                DisplayOrder = 2,
                SeName = SeoExtensions.GenerateSlug("13.0''", false, false, false),
            });
            sa1.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "13.3''",
                DisplayOrder = 3,
                SeName = SeoExtensions.GenerateSlug("13.3''", false, false, false),
            });
            sa1.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "14.0''",
                DisplayOrder = 4,
                SeName = SeoExtensions.GenerateSlug("14.0''", false, false, false),
            });
            sa1.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "15.0''",
                DisplayOrder = 4,
                SeName = SeoExtensions.GenerateSlug("15.0''", false, false, false),
            });
            sa1.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "15.6''",
                DisplayOrder = 5,
                SeName = SeoExtensions.GenerateSlug("15.6''", false, false, false),
            });
            await _specificationAttributeRepository.UpdateAsync(sa1);

            var sa2 = new SpecificationAttribute
            {
                Name = "CPU Type",
                DisplayOrder = 2,
                SeName = SeoExtensions.GenerateSlug("CPU Type", false, false, false),
            };
            await _specificationAttributeRepository.InsertAsync(sa2);

            sa2.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "Intel Core i5",
                DisplayOrder = 1,
                SeName = SeoExtensions.GenerateSlug("Intel Core i5", false, false, false),
            });

            sa2.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "Intel Core i7",
                DisplayOrder = 2,
                SeName = SeoExtensions.GenerateSlug("Intel Core i7", false, false, false),
            });
            await _specificationAttributeRepository.UpdateAsync(sa2);

            var sa3 = new SpecificationAttribute
            {
                Name = "Memory",
                DisplayOrder = 3,
                SeName = SeoExtensions.GenerateSlug("Memory", false, false, false),
            };
            await _specificationAttributeRepository.InsertAsync(sa3);

            sa3.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "4 GB",
                DisplayOrder = 1,
                SeName = SeoExtensions.GenerateSlug("4 GB", false, false, false),
            });
            sa3.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "8 GB",
                DisplayOrder = 2,
                SeName = SeoExtensions.GenerateSlug("8 GB", false, false, false),
            });
            sa3.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "16 GB",
                DisplayOrder = 3,
                SeName = SeoExtensions.GenerateSlug("16 GB", false, false, false),
            });
            await _specificationAttributeRepository.UpdateAsync(sa3);

            var sa4 = new SpecificationAttribute
            {
                Name = "Hardrive",
                DisplayOrder = 5,
                SeName = SeoExtensions.GenerateSlug("Hardrive", false, false, false),
            };
            await _specificationAttributeRepository.InsertAsync(sa4);

            sa4.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "128 GB",
                DisplayOrder = 7,
                SeName = SeoExtensions.GenerateSlug("128 GB", false, false, false),
            });
            sa4.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "500 GB",
                DisplayOrder = 4,
                SeName = SeoExtensions.GenerateSlug("500 GB", false, false, false),
            });
            sa4.SpecificationAttributeOptions.Add(new SpecificationAttributeOption
            {
                Name = "1 TB",
                DisplayOrder = 3,
                SeName = SeoExtensions.GenerateSlug("1 TB", false, false, false),
            });
            await _specificationAttributeRepository.UpdateAsync(sa4);
        }
    }
}
