using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Orders;

public class ChangeMerchandiseReturnStatusCommand : IRequest<string?>
{
    public required string Id { get; set; }
    public int Status { get; set; }
    public string? CustomerNote { get; set; }
    public string? ChangedBy { get; set; }
}
