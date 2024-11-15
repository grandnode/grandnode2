using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddProductSpecificationCommand : IRequest<bool>
{
    public ProductDto Product { get; set; }
    public ProductSpecificationAttributeDto Model { get; set; }
}