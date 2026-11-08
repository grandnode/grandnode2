using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    /// <summary>
    ///     Every inventory document books stock in a warehouse, so an installation without sample data still gets one
    /// </summary>
    protected virtual Task InstallWarehouses(bool installSampleData)
    {
        if (!installSampleData)
        {
            _warehouseRepository.Insert(new Warehouse { Code = "WHS-MAIN", Name = "Main warehouse" });
            return Task.CompletedTask;
        }

        var germany = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "DE");
        var euWarehouseAddress = new Address {
            Address1 = "Speicherstadt Kai 12",
            City = "Hamburg",
            CountryId = germany?.Id,
            ZipPostalCode = "20457"
        };

        var usa = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "US");
        var usWarehouseAddress = new Address {
            Address1 = "4500 Aircenter Circle",
            City = "Reno",
            StateProvinceId = usa?.StateProvinces.FirstOrDefault(sp => sp.Name == "Nevada")?.Id,
            CountryId = usa?.Id,
            ZipPostalCode = "89502"
        };

        var warehouses = new List<Warehouse> {
            new() {
                Code = "WHS-EU",
                Name = "EU Fulfilment Centre",
                Address = euWarehouseAddress,
                DisplayOrder = 0
            },
            new() {
                Code = "WHS-US",
                Name = "US Fulfilment Centre",
                Address = usWarehouseAddress,
                DisplayOrder = 1
            }
        };

        warehouses.ForEach(x => _warehouseRepository.Insert(x));
        return Task.CompletedTask;
    }
}
