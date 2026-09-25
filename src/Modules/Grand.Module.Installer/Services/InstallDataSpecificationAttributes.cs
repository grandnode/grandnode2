using Grand.Domain.Catalog;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallSpecificationAttributes()
    {
        var sa1 = new SpecificationAttribute {
            Name = "Color family",
            DisplayOrder = 1,
            SeName = SeoExtensions.GenerateSlug("Color family", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa1);

        var sa1Options = new[] {
            "Black", "White", "Grey", "Beige", "Brown", "Green", "Blue", "Metallic", "Multicolor"
        };
        for (var i = 0; i < sa1Options.Length; i++)
            sa1.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa1Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa1Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa1);

        var sa2 = new SpecificationAttribute {
            Name = "Material",
            DisplayOrder = 2,
            SeName = SeoExtensions.GenerateSlug("Material", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa2);

        var sa2Options = new[] {
            "Cotton", "Organic cotton", "Linen", "Merino wool", "Wool felt", "Leather", "Waxed cotton",
            "Denim", "Recycled nylon", "Recycled polyester", "Down", "Stoneware", "Ceramic", "Walnut wood",
            "Stainless steel", "Aluminium", "Titanium", "Brass", "Steel", "Glass", "Paper", "Silicone"
        };
        for (var i = 0; i < sa2Options.Length; i++)
            sa2.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa2Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa2Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa2);

        var sa3 = new SpecificationAttribute {
            Name = "Connectivity",
            DisplayOrder = 3,
            SeName = SeoExtensions.GenerateSlug("Connectivity", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa3);

        var sa3Options = new[] {
            "Bluetooth 5.3", "Wi-Fi", "Zigbee", "USB-C", "Wired", "NFC"
        };
        for (var i = 0; i < sa3Options.Length; i++)
            sa3.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa3Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa3Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa3);

        var sa4 = new SpecificationAttribute {
            Name = "Battery life",
            DisplayOrder = 4,
            SeName = SeoExtensions.GenerateSlug("Battery life", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa4);

        var sa4Options = new[] {
            "Under 12 hours", "12 to 24 hours", "24 to 48 hours", "2 to 7 days", "Over 7 days"
        };
        for (var i = 0; i < sa4Options.Length; i++)
            sa4.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa4Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa4Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa4);

        var sa5 = new SpecificationAttribute {
            Name = "Capacity",
            DisplayOrder = 5,
            SeName = SeoExtensions.GenerateSlug("Capacity", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa5);

        var sa5Options = new[] {
            "Up to 0.5 L", "0.5 to 1.5 L", "1.5 to 10 L", "10 to 30 L", "Over 30 L", "2 persons"
        };
        for (var i = 0; i < sa5Options.Length; i++)
            sa5.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa5Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa5Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa5);

        var sa6 = new SpecificationAttribute {
            Name = "Fit",
            DisplayOrder = 6,
            SeName = SeoExtensions.GenerateSlug("Fit", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa6);

        var sa6Options = new[] {
            "Slim", "Regular", "Relaxed", "Oversized"
        };
        for (var i = 0; i < sa6Options.Length; i++)
            sa6.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa6Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa6Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa6);

        var sa7 = new SpecificationAttribute {
            Name = "Season",
            DisplayOrder = 7,
            SeName = SeoExtensions.GenerateSlug("Season", false, false, false)
        };
        await _specificationAttributeRepository.InsertAsync(sa7);

        var sa7Options = new[] {
            "Spring", "Summer", "Autumn", "Winter", "All season", "Three season"
        };
        for (var i = 0; i < sa7Options.Length; i++)
            sa7.SpecificationAttributeOptions.Add(new SpecificationAttributeOption {
                Name = sa7Options[i],
                DisplayOrder = i + 1,
                SeName = SeoExtensions.GenerateSlug(sa7Options[i], false, false, false)
            });
        await _specificationAttributeRepository.UpdateAsync(sa7);
    }
}
