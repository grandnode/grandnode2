using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Orders;

public class DeleteMerchandiseReturnCommand : IRequest<string>
{
    public string Id { get; set; }
}
