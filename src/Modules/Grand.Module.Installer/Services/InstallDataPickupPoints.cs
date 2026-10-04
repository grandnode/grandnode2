using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallPickupPoints()
    {
        var euWarehouseId = _warehouseRepository.Table.Single(x => x.Name == "EU Fulfilment Centre").Id;
        var usWarehouseId = _warehouseRepository.Table.Single(x => x.Name == "US Fulfilment Centre").Id;

        var denmark = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "DK");
        var copenhagenAddress = new Address {
            Address1 = "Vesterbrogade 68",
            City = "Copenhagen",
            CountryId = denmark?.Id,
            ZipPostalCode = "1620"
        };

        var usa = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "US");
        var portlandAddress = new Address {
            Address1 = "812 NW Couch Street",
            City = "Portland",
            StateProvinceId = usa?.StateProvinces.FirstOrDefault(sp => sp.Name == "Oregon")?.Id,
            CountryId = usa?.Id,
            ZipPostalCode = "97209"
        };

        var pickupPoints = new List<PickupPoint> {
            new() {
                Name = "Copenhagen Studio Pickup",
                Address = copenhagenAddress,
                WarehouseId = euWarehouseId,
                PickupFee = 0
            },
            new() {
                Name = "Portland Workshop Pickup",
                Address = portlandAddress,
                WarehouseId = usWarehouseId,
                PickupFee = 2.5
            }
        };

        foreach (var point in pickupPoints)
            await _pickupPointsRepository.InsertAsync(point);
    }
}
