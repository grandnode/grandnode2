using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Orders;

public class GetMerchandiseReturns : IRequest<CustomerMerchandiseReturnsModel>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Language Language { get; set; }
    public Currency Currency { get; set; }

    /// <summary>
    ///     The order whose returns are listed, for the guest a grant opened it to; null lists the customer's returns
    /// </summary>
    public string OrderId { get; set; }
}