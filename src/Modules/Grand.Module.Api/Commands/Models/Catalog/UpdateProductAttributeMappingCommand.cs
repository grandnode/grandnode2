using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductAttributeMappingCommand : IRequest<ProductAttributeMappingDto>
{
    public ProductDto Product { get; set; }
    public ProductAttributeMappingDto Model { get; set; }
}