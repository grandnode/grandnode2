using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductCommand : IRequest<ProductDto?>
{
    public required ProductDto Model { get; set; }
}