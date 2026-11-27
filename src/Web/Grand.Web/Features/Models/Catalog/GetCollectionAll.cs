using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetCollectionAll : IRequest<CollectionListModel>
{
    public required Domain.Stores.Store Store { get; set; }
    public required Customer Customer { get; set; }
    public required Language Language { get; set; }
    public required CollectionPagingModel Command { get; set; }
}