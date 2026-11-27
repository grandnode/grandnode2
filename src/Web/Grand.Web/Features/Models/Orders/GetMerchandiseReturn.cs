using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Orders;

public class GetMerchandiseReturn : IRequest<MerchandiseReturnModel>
{
    public required Order Order { get; set; }
    public required Language Language { get; set; }
    public required Domain.Stores.Store Store { get; set; }

    /// <summary>
    ///     The customer returning the items, whose addresses are offered for the pickup
    /// </summary>
    public required Customer Customer { get; set; }
}