using Grand.Business.Common.Extensions;
using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Seo;
using Grand.Domain.Vendors;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallVendors()
        {
            var vendors = new List<Vendor>
            {
                new Vendor
                {
                    Name = "Vendor 1",
                    Email = "vendor1email@gmail.com",
                    Description = "Some description...",
                    AdminComment = "",
                    PictureId = "",
                    Active = true,
                    DisplayOrder = 1,
                    PageSize = 6,
                    AllowCustomersToSelectPageSize = true,
                    PageSizeOptions = "6, 3, 9, 18",
                },
                new Vendor
                {
                    Name = "Vendor 2",
                    Email = "vendor2email@gmail.com",
                    Description = "Some description...",
                    AdminComment = "",
                    PictureId = "",
                    Active = true,
                    DisplayOrder = 2,
                    PageSize = 6,
                    AllowCustomersToSelectPageSize = true,
                    PageSizeOptions = "6, 3, 9, 18",
                }
            };

            await _vendorRepository.InsertAsync(vendors);

            //search engine names
            foreach (var vendor in vendors)
            {
                var seName = SeoExtensions.GenerateSlug(vendor.Name, false, false, false);
                await _entityUrlRepository.InsertAsync(new EntityUrl
                {
                    EntityId = vendor.Id,
                    EntityName = "Vendor",
                    LanguageId = "",
                    IsActive = true,
                    Slug = seName
                });
                vendor.SeName = seName;
                await _vendorRepository.UpdateAsync(vendor);
            }
        }
    }
}
