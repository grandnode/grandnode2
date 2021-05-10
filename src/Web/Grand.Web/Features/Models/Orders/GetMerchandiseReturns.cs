using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Models.Orders
{
    public class GetMerchandiseReturns : IRequest<CustomerMerchandiseReturnsModel>
    {
        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public Language Language { get; set; }
    }
}
