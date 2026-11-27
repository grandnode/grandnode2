using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetChildCategoryIds : IRequest<IList<string>>
{
    public required string ParentCategoryId { get; set; }
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
}