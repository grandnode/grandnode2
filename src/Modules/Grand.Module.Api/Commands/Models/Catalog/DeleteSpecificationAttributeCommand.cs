using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class DeleteSpecificationAttributeCommand : IRequest<bool>
{
    public SpecificationAttributeDto Model { get; set; }
}