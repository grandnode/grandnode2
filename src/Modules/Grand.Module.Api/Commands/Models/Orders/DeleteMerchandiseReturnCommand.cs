using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Orders;

public class DeleteMerchandiseReturnCommand : IRequest<string?>
{
    public required string Id { get; set; }
}
