using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Common.UserAgent;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     Crawlers run as the SearchEngine system customer
/// </summary>
public class SearchEngineCustomerResolver(
    IUserAgentClassifier userAgentClassifier,
    ICustomerService customerService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.SearchEngine;

    public async Task<Customer> Resolve()
    {
        if (!userAgentClassifier.IsCrawler) return null;

        return await customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
    }
}
