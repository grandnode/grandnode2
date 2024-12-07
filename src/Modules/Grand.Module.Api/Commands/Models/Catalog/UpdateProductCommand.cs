using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateProductCommand : IRequest<ProductDto>
{
    public ProductDto Model { get; set; }
}