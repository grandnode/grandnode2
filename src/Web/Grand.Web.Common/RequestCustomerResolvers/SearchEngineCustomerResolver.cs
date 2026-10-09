using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Wangkanai.Detection.Services;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     Crawlers run as the SearchEngine system customer
/// </summary>
public class SearchEngineCustomerResolver(
    IDetectionService detectionService,
    ICustomerService customerService) : IRequestCustomerResolver
{
    public int Order => RequestCustomerResolverOrder.SearchEngine;

    public async Task<Customer> Resolve(Store store)
    {
        var isCrawler = detectionService.Crawler?.IsCrawler;
        if (!isCrawler.GetValueOrDefault()) return null;

        return await customerService.GetCustomerBySystemName(SystemCustomerNames.SearchEngine);
    }
}
