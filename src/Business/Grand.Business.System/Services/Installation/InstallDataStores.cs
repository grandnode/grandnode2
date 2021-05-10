using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallStores(string companyName, string companyAddress, string companyPhoneNumber, string companyEmail)
        {
            var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
            var storeUrl = $"{httpContextAccessor.HttpContext?.Request?.Scheme}://{httpContextAccessor.HttpContext?.Request?.Host}";
            var stores = new List<Store>
            {
                new Store
                {
                    Name = "Your store name",
                    Shortcut = "Store",
                    Url = storeUrl,
                    SslEnabled = false,
                    Hosts = "yourstore.com,www.yourstore.com",
                    DisplayOrder = 1,
                    CompanyName = companyName,
                    CompanyAddress = companyAddress,
                    CompanyPhoneNumber = companyPhoneNumber,
                    CompanyVat = null,
                    CompanyEmail = companyEmail,
                    CompanyHours = "Monday - Sunday / 8:00AM - 6:00PM"
                },
            };

            await _storeRepository.InsertAsync(stores);
        }
    }
}
