using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Models.Orders;

/// <summary>
///     The ways of sending back the items with the quantities, reasons and actions chosen in the return form
/// </summary>
public class GetMerchandiseReturnShippingOffers : IRequest<IList<ReturnShippingOfferModel>>
{
    public Order Order { get; set; }
    public MerchandiseReturnModel Model { get; set; }
    public Address PickupAddress { get; set; }
    public Customer Customer { get; set; }
    public Language Language { get; set; }
    public Currency Currency { get; set; }

    /// <summary>
    ///     Turns the route name of an option's form into its address
    /// </summary>
    public Func<string, string> FormUrl { get; set; }
}
