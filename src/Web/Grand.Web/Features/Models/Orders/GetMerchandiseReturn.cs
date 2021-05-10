using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Models.Orders
{
    public class GetMerchandiseReturn : IRequest<MerchandiseReturnModel>
    {
        public Order Order { get; set; }
        public Language Language { get; set; }
        public Store Store { get; set; }
    }
}
