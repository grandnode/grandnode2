using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Interfaces
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
