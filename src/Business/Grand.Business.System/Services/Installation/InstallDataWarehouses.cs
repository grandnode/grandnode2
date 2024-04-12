using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallWarehouses()
    {
        var country = _countryRepository.Table.FirstOrDefault(c => c.ThreeLetterIsoCode == "USA");
        var warehouse1address = new Address {
            Address1 = "21 West 52nd Street",
            City = "New York",
            StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "New York")?.Id,
            CountryId = country?.Id,
            ZipPostalCode = "10021"
        };

        var warehouse2address = new Address {
            Address1 = "300 South Spring Stree",
            City = "Los Angeles",
            StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "California")?.Id,
            CountryId = country?.Id,
            ZipPostalCode = "90013"
        };

        var warehouses = new List<Warehouse> {
            new() {
                Code = "WHS01",
                Name = "Warehouse 1",
                Address = warehouse1address,
                DisplayOrder = 0
            },
            new() {
                Code = "WHS02",
                Name = "Warehouse 2",
                Address = warehouse2address,
                DisplayOrder = 1
            }
        };

        warehouses.ForEach(x => _warehouseRepository.Insert(x));
        return Task.CompletedTask;
    }
}