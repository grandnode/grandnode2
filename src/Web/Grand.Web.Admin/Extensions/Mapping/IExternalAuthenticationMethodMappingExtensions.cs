using Grand.Business.Authentication.Interfaces;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.ExternalAuthentication;

namespace Grand.Web.Admin.Extensions
{
    public static class IExternalAuthenticationMethodMappingExtensions
    {
        public static AuthenticationMethodModel ToModel(this IExternalAuthenticationProvider entity)
        {
            return entity.MapTo<IExternalAuthenticationProvider, AuthenticationMethodModel>();
        }
    }
}