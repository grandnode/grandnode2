using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Common.UserAgent;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     Crawlers run as the SearchEngine system customer
/// </summary>
public class SearchEngineCustomerResolver(
    IUserAgentClassifier userAgentClassifier,
    ICustomerService customerService,
    IHttpContextAccessor httpContextAccessor) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.SearchEngine;

    public async Task<Customer> Resolve()
    {
        //an authenticated API client is never treated as a crawler
        var request = httpContextAccessor.HttpContext?.Request;
        if (request != null && !string.IsNullOrEmpty(request.Headers.Authorization.ToString())) return null;

        if (!userAgentClassifier.IsCrawler) return null;

        return await customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
    }
}
