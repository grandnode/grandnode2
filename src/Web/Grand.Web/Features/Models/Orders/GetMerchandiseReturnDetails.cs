using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using MediatR;

namespace Grand.Web.Features.Models.Orders
{
    public class GetMerchandiseReturnDetails : IRequest<MerchandiseReturnDetailsModel>
    {
        public MerchandiseReturn MerchandiseReturn { get; set; }
        public Order Order { get; set; }
        public Language Language { get; set; }
    }
}
