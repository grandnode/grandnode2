using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class
    UpdateSpecificationAttributeCommandHandler : IRequestHandler<UpdateSpecificationAttributeCommand,
    SpecificationAttributeDto>
{
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public UpdateSpecificationAttributeCommandHandler(ISpecificationAttributeService specificationAttributeService)
    {
        _specificationAttributeService = specificationAttributeService;
    }

    public async Task<SpecificationAttributeDto> Handle(UpdateSpecificationAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var specificationAttribute =
            await _specificationAttributeService.GetSpecificationAttributeById(request.Model.Id);
        foreach (var option in specificationAttribute.SpecificationAttributeOptions)
            if (request.Model.SpecificationAttributeOptions.FirstOrDefault(x => x.Id == option.Id) == null)
                await _specificationAttributeService.DeleteSpecificationAttributeOption(option);
        specificationAttribute = request.Model.ToEntity(specificationAttribute);
        await _specificationAttributeService.UpdateSpecificationAttribute(specificationAttribute);
        return specificationAttribute.ToModel();
    }
}