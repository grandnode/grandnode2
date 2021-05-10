using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Commands.Models.Orders
{
    public class MerchandiseReturnSubmitCommand : IRequest<(MerchandiseReturnModel model, MerchandiseReturn rr)>
    {
        public MerchandiseReturnModel Model { get; set; }
        public Order Order { get; set; }
        public Address Address { get; set; }
        public IFormCollection Form { get; set; }
    }
}
