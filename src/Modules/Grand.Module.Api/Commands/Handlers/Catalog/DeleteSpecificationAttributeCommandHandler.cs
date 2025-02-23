using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteSpecificationAttributeCommandHandler : IRequestHandler<DeleteSpecificationAttributeCommand, bool>
{
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public DeleteSpecificationAttributeCommandHandler(ISpecificationAttributeService specificationAttributeService)
    {
        _specificationAttributeService = specificationAttributeService;
    }

    public async Task<bool> Handle(DeleteSpecificationAttributeCommand request, CancellationToken cancellationToken)
    {
        var specificationAttribute =
            await _specificationAttributeService.GetSpecificationAttributeById(request.Model.Id);
        if (specificationAttribute != null)
            await _specificationAttributeService.DeleteSpecificationAttribute(specificationAttribute);
        return true;
    }
}