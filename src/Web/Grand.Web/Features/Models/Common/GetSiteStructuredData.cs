using Grand.Web.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Common;

public class GetSiteStructuredData : IRequest<SiteStructuredDataModel>
{
    public Domain.Stores.Store Store { get; set; }
}
