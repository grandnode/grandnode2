using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddProductSpecificationCommand : IRequest<bool>
{
    public required ProductDto Product { get; set; }
    public required ProductSpecificationAttributeDto Model { get; set; }
}