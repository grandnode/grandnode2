using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallWarehouses()
        {
            var country = _countryRepository.Table.FirstOrDefault(c => c.ThreeLetterIsoCode == "USA");
            var warehouse1address = new Address
            {
                Address1 = "21 West 52nd Street",
                City = "New York",
                StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "New York")?.Id,
                CountryId = country?.Id,
                ZipPostalCode = "10021",
                CreatedOnUtc = DateTime.UtcNow,
            };

            var warehouse2address = new Address
            {
                Address1 = "300 South Spring Stree",
                City = "Los Angeles",
                StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "California").Id,
                CountryId = country.Id,
                ZipPostalCode = "90013",
                CreatedOnUtc = DateTime.UtcNow,
            };

            var warehouses = new List<Warehouse>
            {
                new Warehouse
                {
                    Name = "Warehouse 1 (New York)",
                    Address = warehouse1address,
                    DisplayOrder = 0,
                },
                new Warehouse
                {
                    Name = "Warehouse 2 (Los Angeles)",
                    Address = warehouse2address,
                    DisplayOrder = 1,
                }
            };

            await _warehouseRepository.InsertAsync(warehouses);
        }
    }
}
