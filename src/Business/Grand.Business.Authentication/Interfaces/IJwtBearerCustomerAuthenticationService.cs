using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Interfaces
{
    public interface IJwtBearerCustomerAuthenticationService
    {
        Task<bool> Valid(TokenValidatedContext context);
        Task<string> ErrorMessage();
    }
}
