using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetMenu : IRequest<MenuModel>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Language Language { get; set; }
}