using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Domain.Vendors;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallVendors()
    {
        var denmark = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "DK");
        var austria = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "AT");
        var usa = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "US");

        var vendors = new List<Vendor> {
            new() {
                Name = "Nordic Craft Collective",
                Email = "studio@nordiccraft.example",
                Description = "<p>We are a small collective of Danish potters and woodworkers who share a courtyard " +
                              "studio in Copenhagen. Every piece that leaves our workbenches is thrown, glazed, " +
                              "sanded, or finished by hand, in small batches that we can stand behind one at a " +
                              "time.</p>" +
                              "<p>We started out swapping kiln time and shop tips, and grew into a proper " +
                              "cooperative without losing the parts that made us want to do this in the first " +
                              "place: honest materials, visible tool marks, and pieces made to be used every " +
                              "day rather than kept behind glass.</p>",
                Commission = 12,
                AllowCustomerReviews = true,
                Active = true,
                DisplayOrder = 1,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Address = new Address {
                    Address1 = "Jaegersborggade 14",
                    City = "Copenhagen N",
                    ZipPostalCode = "2200",
                    CountryId = denmark?.Id
                }
            },
            new() {
                Name = "Alpine Rental Co.",
                Email = "rentals@alpinerental.example",
                Description = "<p>We rent out the gear that turns a weekend in the mountains into a trip worth " +
                              "remembering: tents, sleeping systems, and touring e-bikes, all serviced and ready " +
                              "to collect from our shop in Innsbruck the morning you arrive.</p>" +
                              "<p>Everything in our fleet gets checked, cleaned, and tuned between every rental, " +
                              "so what you pick up works the way it should on day one, not just on the day we " +
                              "bought it. Book by the day, keep it for the week if the weather is good.</p>",
                Commission = 15,
                AllowCustomerReviews = true,
                Active = true,
                DisplayOrder = 2,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Address = new Address {
                    Address1 = "Anichstrasse 22",
                    City = "Innsbruck",
                    ZipPostalCode = "6020",
                    CountryId = austria?.Id
                }
            },
            new() {
                Name = "Vintage Velo",
                Email = "bikes@vintagevelo.example",
                Description = "<p>We restore steel bikes the slow way: strip the frame, save what is worth " +
                              "saving, and rebuild around it with period-correct or better parts. Most of what " +
                              "passes through our Portland workshop rolls back out on the street, not into a " +
                              "display case.</p>" +
                              "<p>A handful of the rarest frames that cross our bench go to auction instead of " +
                              "sale, once or twice a year, for collectors chasing something we may only see once. " +
                              "Everything else is priced to ride.</p>",
                Commission = 10,
                AllowCustomerReviews = true,
                Active = true,
                DisplayOrder = 3,
                PageSize = 12,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12, 24, 36",
                Address = new Address {
                    Address1 = "1420 SE Division Street",
                    City = "Portland",
                    ZipPostalCode = "97202",
                    CountryId = usa?.Id,
                    StateProvinceId = usa?.StateProvinces.FirstOrDefault(sp => sp.Name == "Oregon")?.Id
                }
            }
        };

        foreach (var vendor in vendors)
            _vendorRepository.Insert(vendor);

        var images = new Dictionary<string, string> {
            ["Nordic Craft Collective"] = "vendor_nordic_craft_collective.jpg",
            ["Alpine Rental Co."] = "vendor_alpine_rental_co.jpg",
            ["Vintage Velo"] = "vendor_vintage_velo.jpg"
        };

        foreach (var vendor in vendors)
        {
            var picture = await InsertSamplePicture(images[vendor.Name], vendor.Name, Reference.Vendor, vendor.Id);
            vendor.PictureId = picture.Id;
            await InsertSlug(vendor.Id, EntityTypes.Vendor, vendor.Name, seName => vendor.SeName = seName);
            await _vendorRepository.UpdateAsync(vendor);
        }
    }
}
