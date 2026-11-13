using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Orders;

public class MerchandiseReturnSubmitCommand : IRequest<MerchandiseReturnSubmitResult>
{
    public MerchandiseReturnModel Model { get; set; }
    public Order Order { get; set; }
    public Address Address { get; set; }

    /// <summary>
    ///     The fields posted with the form; those of the chosen return method's form go under the "returnshipping."
    ///     prefix
    /// </summary>
    public IDictionary<string, string> Form { get; set; } = new Dictionary<string, string>();
}