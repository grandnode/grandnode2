using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Common;

public class GetSitemap : IRequest<SitemapModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Language Language { get; set; }
}