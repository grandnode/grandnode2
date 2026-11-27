using Grand.Mediator;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Commands.Models.Orders;

public class DeleteMerchandiseReturnReasonCommand : IRequest<bool>
{
    public required MerchandiseReturnReasonDto Model { get; set; }
}
