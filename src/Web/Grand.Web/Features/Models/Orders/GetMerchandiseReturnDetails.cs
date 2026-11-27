using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Orders;

public class GetMerchandiseReturnDetails : IRequest<MerchandiseReturnDetailsModel>
{
    public required MerchandiseReturn MerchandiseReturn { get; set; }
    public required Order Order { get; set; }
    public required Language Language { get; set; }
}