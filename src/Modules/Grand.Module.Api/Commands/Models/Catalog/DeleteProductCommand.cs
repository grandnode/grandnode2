using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteProductCommand : IRequest<bool>
{
    public required ProductDto Model { get; set; }
}