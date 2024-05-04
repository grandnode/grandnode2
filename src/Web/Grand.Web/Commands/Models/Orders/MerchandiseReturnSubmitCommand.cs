using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Commands.Models.Orders;

public class MerchandiseReturnSubmitCommand : IRequest<MerchandiseReturn>
{
    public MerchandiseReturnModel Model { get; set; }
    public Order Order { get; set; }
    public Address Address { get; set; }
}