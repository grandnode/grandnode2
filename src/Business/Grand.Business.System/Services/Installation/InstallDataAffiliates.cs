using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Affiliates;
using Grand.Domain.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallAffiliates()
        {
            var country = _countryRepository.Table.FirstOrDefault(c => c.ThreeLetterIsoCode == "USA");
            var affiliateAddress = new Address
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "affiliate_email@gmail.com",
                Company = "Company name here...",
                City = "New York",
                Address1 = "21 West 52nd Street",
                ZipPostalCode = "10021",
                PhoneNumber = "123456789",
                StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "New York")?.Id,
                CountryId = country.Id,
                CreatedOnUtc = DateTime.UtcNow,
            };
            var affilate = new Affiliate
            {
                Active = true,
                Address = affiliateAddress
            };
            await _affiliateRepository.InsertAsync(affilate);
        }
    }
}
