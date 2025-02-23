using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class
    AddSpecificationAttributeCommandHandler : IRequestHandler<AddSpecificationAttributeCommand,
    SpecificationAttributeDto>
{
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public AddSpecificationAttributeCommandHandler(
        ISpecificationAttributeService specificationAttributeService)
    {
        _specificationAttributeService = specificationAttributeService;
    }

    public async Task<SpecificationAttributeDto> Handle(AddSpecificationAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var specificationAttribute = request.Model.ToEntity();
        await _specificationAttributeService.InsertSpecificationAttribute(specificationAttribute);

        return specificationAttribute.ToModel();
    }
}