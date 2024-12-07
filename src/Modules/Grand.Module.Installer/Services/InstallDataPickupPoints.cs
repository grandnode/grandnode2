using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallPickupPoints()
    {
        var country = _countryRepository.Table.FirstOrDefault(c => c.ThreeLetterIsoCode == "USA");
        var addresspoint = new Address {
            Address1 = "21 West 52nd Street",
            City = "New York",
            StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "New York")?.Id,
            CountryId = country?.Id,
            ZipPostalCode = "10021"
        };

        var point = new PickupPoint {
            Address = addresspoint,
            Name = "My Store - New York"
        };
        await _pickupPointsRepository.InsertAsync(point);
    }
}