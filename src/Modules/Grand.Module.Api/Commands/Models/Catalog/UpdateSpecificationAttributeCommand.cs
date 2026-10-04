using Grand.Module.Api.DTOs.Catalog;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class UpdateSpecificationAttributeCommand : IRequest<SpecificationAttributeDto>
{
    public SpecificationAttributeDto Model { get; set; }
}