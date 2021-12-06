using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallStores(
            string httpscheme, HostString host,
            string companyName, string companyAddress, string companyPhoneNumber, string companyEmail)
        {
            var store =
                new Store {
                    Name = "Your store name",
                    Shortcut = "Store",
                    Url = $"http://{host}/",
                    SecureUrl = $"https://{host}/",
                    SslEnabled = httpscheme.ToLowerInvariant() == "https",
                    DisplayOrder = 1,
                    CompanyName = companyName,
                    CompanyAddress = companyAddress,
                    CompanyPhoneNumber = companyPhoneNumber,
                    CompanyRegNo = null,
                    CompanyVat = null,
                    CompanyEmail = companyEmail,
                    CompanyHours = "Monday - Sunday / 8:00AM - 6:00PM",
                    Domains = new List<DomainHost>() { new DomainHost() { HostName = host.Value, Url = $"{httpscheme}://{host}/", Primary = true } }
                };

            store = await _storeRepository.InsertAsync(store);

            await InstallDataRobotsTxt(store);
        }
    }
}
