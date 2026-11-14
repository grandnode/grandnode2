using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Commands.Models.Orders;

public class MerchandiseReturnSubmitCommand : IRequest<MerchandiseReturnSubmitResult>
{
    public MerchandiseReturnModel Model { get; set; }
    public Order Order { get; set; }
    public Address Address { get; set; }

    /// <summary>
    ///     The customer the return is saved for: the one placing it, or the guest of the order a grant opened
    /// </summary>
    public Customer Customer { get; set; }

    /// <summary>
    ///     The fields posted with the form; those of the chosen return method's form go under the "returnshipping."
    ///     prefix
    /// </summary>
    public IDictionary<string, string> Form { get; set; } = new Dictionary<string, string>();

    /// <summary>
    ///     The photos posted with the form, by the id of the order item they show
    /// </summary>
    public IDictionary<string, IList<IFormFile>> Photos { get; set; } = new Dictionary<string, IList<IFormFile>>();
}