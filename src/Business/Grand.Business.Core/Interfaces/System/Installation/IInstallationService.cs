using Microsoft.AspNetCore.Http;

namespace Grand.Business.Core.Interfaces.System.Installation
{
    public partial interface IInstallationService
    {
        Task InstallData(
            string httpscheme, HostString host,
            string defaultUserEmail, string defaultUserPassword, string collation, bool installSampleData = true, 
            string companyName = "", string companyAddress = "", string companyPhoneNumber = "", string companyEmail = "");
    }
}
