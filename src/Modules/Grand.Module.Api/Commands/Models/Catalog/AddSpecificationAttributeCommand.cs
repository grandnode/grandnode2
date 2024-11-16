using Grand.Module.Api.DTOs.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Catalog;

public class AddSpecificationAttributeCommand : IRequest<SpecificationAttributeDto>
{
    public SpecificationAttributeDto Model { get; set; }
}