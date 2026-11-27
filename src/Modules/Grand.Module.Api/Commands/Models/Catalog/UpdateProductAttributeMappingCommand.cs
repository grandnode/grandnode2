using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductAttributeMappingCommand : IRequest<ProductAttributeMappingDto>
{
    public required ProductDto Product { get; set; }
    public required ProductAttributeMappingDto Model { get; set; }
}