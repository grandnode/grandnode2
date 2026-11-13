using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Models.Orders;

/// <summary>
///     The refund the return form comes to, with the return method chosen in it
/// </summary>
public class GetMerchandiseReturnEstimate : IRequest<MerchandiseReturnEstimateModel>
{
    public Order Order { get; set; }
    public MerchandiseReturnModel Model { get; set; }
    public Address PickupAddress { get; set; }
    public Customer Customer { get; set; }
    public Language Language { get; set; }

    /// <summary>
    ///     The fields posted with the form; those of the chosen return method's form go under the "returnshipping."
    ///     prefix
    /// </summary>
    public IDictionary<string, string> Form { get; set; } = new Dictionary<string, string>();
}
