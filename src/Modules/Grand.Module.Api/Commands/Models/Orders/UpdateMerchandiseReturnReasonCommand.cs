using Grand.Mediator;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Commands.Models.Orders;

public class UpdateMerchandiseReturnReasonCommand : IRequest<MerchandiseReturnReasonDto>
{
    public MerchandiseReturnReasonDto Model { get; set; }
}
