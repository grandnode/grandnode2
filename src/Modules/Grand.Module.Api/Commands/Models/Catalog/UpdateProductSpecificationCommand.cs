using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductSpecificationCommand : IRequest<bool>
{
    public ProductDto Product { get; set; }
    public ProductSpecificationAttributeDto Model { get; set; }
}