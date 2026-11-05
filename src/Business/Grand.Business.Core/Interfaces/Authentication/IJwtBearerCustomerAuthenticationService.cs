using System.Security.Claims;

namespace Grand.Business.Core.Interfaces.Authentication;

public interface IJwtBearerCustomerAuthenticationService
{
    Task<bool> Valid(ClaimsPrincipal principal);
    Task<string> ErrorMessage();
}