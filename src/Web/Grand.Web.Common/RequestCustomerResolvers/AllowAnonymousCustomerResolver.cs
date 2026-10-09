using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     Endpoints marked [AllowAnonymous] run as the Anonymous system customer
/// </summary>
public class AllowAnonymousCustomerResolver(
    IHttpContextAccessor httpContextAccessor,
    ICustomerService customerService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.AllowAnonymous;

    public async Task<Customer> Resolve(Store store)
    {
        var endpoint = httpContextAccessor.HttpContext?.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() == null) return null;

        return await customerService.GetCustomerBySystemName(SystemCustomerNames.Anonymous);
    }
}
