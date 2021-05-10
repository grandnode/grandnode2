using Grand.Business.Authentication.Utilities;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Interfaces
{
    public interface ISMSVerificationService
    {
        Task<bool> Authenticate(string secretKey, string token, Customer customer);
        Task<TwoFactorCodeSetup> GenerateCode(string secretKey, Customer customer, Language language);
    }
}
