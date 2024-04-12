using Grand.Business.Core.Extensions;
using Grand.Domain.Seo;
using Grand.Domain.Vendors;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual async Task InstallVendors()
    {
        var vendors = new List<Vendor> {
            new() {
                Name = "Vendor 1",
                Email = "vendor1email@gmail.com",
                Description = "Some description...",
                AdminComment = "",
                PictureId = "",
                Active = true,
                DisplayOrder = 1,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9, 18"
            },
            new() {
                Name = "Vendor 2",
                Email = "vendor2email@gmail.com",
                Description = "Some description...",
                AdminComment = "",
                PictureId = "",
                Active = true,
                DisplayOrder = 2,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "6, 3, 9, 18"
            }
        };

        vendors.ForEach(x => _vendorRepository.Insert(x));

        //search engine names
        foreach (var vendor in vendors)
        {
            var seName = SeoExtensions.GenerateSlug(vendor.Name, false, false, false);
            await _entityUrlRepository.InsertAsync(new EntityUrl {
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