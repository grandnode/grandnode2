using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddSpecificationAttributeCommand : IRequest<SpecificationAttributeDto>
{
    public required SpecificationAttributeDto Model { get; set; }
}