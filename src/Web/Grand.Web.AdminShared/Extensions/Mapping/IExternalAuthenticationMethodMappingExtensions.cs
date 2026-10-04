using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.ExternalAuthentication;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class IExternalAuthenticationMethodMappingExtensions
{
    public static AuthenticationMethodModel ToModel(this IExternalAuthenticationProvider entity)
    {
        return entity.MapTo<IExternalAuthenticationProvider, AuthenticationMethodModel>();
    }
}