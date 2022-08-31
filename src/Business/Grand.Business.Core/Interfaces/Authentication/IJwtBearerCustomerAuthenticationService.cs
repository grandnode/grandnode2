using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Grand.Business.Core.Interfaces.Authentication
{
    public interface IJwtBearerCustomerAuthenticationService
    {
        Task<bool> Valid(TokenValidatedContext context);
        Task<string> ErrorMessage();
    }
}
