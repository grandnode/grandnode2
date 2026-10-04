using Grand.Domain.Affiliates;
using Grand.Domain.Common;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallAffiliates()
    {
        var country = _countryRepository.Table.FirstOrDefault(c => c.TwoLetterIsoCode == "US");
        var affiliateAddress = new Address {
            FirstName = "Maya",
            LastName = "Bennett",
            Email = "maya.bennett@affiliatepartner.example",
            Company = "Bennett Digital Media",
            City = "Austin",
            Address1 = "812 Congress Avenue",
            ZipPostalCode = "78701",
            PhoneNumber = "512-555-0142",
            StateProvinceId = country?.StateProvinces.FirstOrDefault(sp => sp.Name == "Texas")?.Id,
            CountryId = country?.Id
        };
        var affiliate = new Affiliate {
            Active = true,
            Address = affiliateAddress
        };
        await _affiliateRepository.InsertAsync(affiliate);
    }
}
