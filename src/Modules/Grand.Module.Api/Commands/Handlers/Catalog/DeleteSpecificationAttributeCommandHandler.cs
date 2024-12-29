using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteSpecificationAttributeCommandHandler : IRequestHandler<DeleteSpecificationAttributeCommand, bool>
{
    private readonly ISpecificationAttributeService _specificationAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public DeleteSpecificationAttributeCommandHandler(
        ISpecificationAttributeService specificationAttributeService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _specificationAttributeService = specificationAttributeService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
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