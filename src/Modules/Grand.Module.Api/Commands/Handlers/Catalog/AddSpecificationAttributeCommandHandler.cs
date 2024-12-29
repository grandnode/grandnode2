using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class
    AddSpecificationAttributeCommandHandler : IRequestHandler<AddSpecificationAttributeCommand,
    SpecificationAttributeDto>
{
    private readonly ISpecificationAttributeService _specificationAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public AddSpecificationAttributeCommandHandler(
        ISpecificationAttributeService specificationAttributeService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _specificationAttributeService = specificationAttributeService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<SpecificationAttributeDto> Handle(AddSpecificationAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var specificationAttribute = request.Model.ToEntity();
        await _specificationAttributeService.InsertSpecificationAttribute(specificationAttribute);

        return specificationAttribute.ToModel();
    }
}