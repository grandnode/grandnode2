using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteProductAttributeCommand : IRequest<bool>
{
    public ProductAttributeDto Model { get; set; }
}