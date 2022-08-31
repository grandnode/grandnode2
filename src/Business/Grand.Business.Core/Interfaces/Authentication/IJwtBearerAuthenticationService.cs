using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Grand.Business.Core.Interfaces.Authentication
{
    public interface IJwtBearerAuthenticationService
    {
        /// <summary>
        /// Valid email 
        /// </summary>
        ///<param name="context">Token</param>
        Task<bool> Valid(TokenValidatedContext context);

        /// <summary>
        /// Get error message
        /// </summary>
        /// <returns></returns>
        Task<string> ErrorMessage();

    }
}
