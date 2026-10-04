using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteProductAttributeCommand : IRequest<bool>
{
    public ProductAttributeDto Model { get; set; }
}