using Grand.Mediator;
using Grand.Module.Api.DTOs.Orders;

namespace Grand.Module.Api.Commands.Models.Orders;

public class UpdateMerchandiseReturnCommand : IRequest<string>
{
    public string Id { get; set; }
    public MerchandiseReturnDto Model { get; set; }
}
