using Grand.Mediator;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Commands.Models.Orders;

public class UpdateMerchandiseReturnCommand : IRequest<string?>
{
    public required string Id { get; set; }
    public required MerchandiseReturnDto Model { get; set; }
}
