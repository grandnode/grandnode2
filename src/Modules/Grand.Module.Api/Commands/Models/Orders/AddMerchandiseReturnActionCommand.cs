using Grand.Mediator;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Commands.Models.Orders;

public class AddMerchandiseReturnActionCommand : IRequest<MerchandiseReturnActionDto>
{
    public required MerchandiseReturnActionDto Model { get; set; }
}
