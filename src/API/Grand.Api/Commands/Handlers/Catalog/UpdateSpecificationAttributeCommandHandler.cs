using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class
    UpdateSpecificationAttributeCommandHandler : IRequestHandler<UpdateSpecificationAttributeCommand,
    SpecificationAttributeDto>
{
    private readonly ISpecificationAttributeService _specificationAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public UpdateSpecificationAttributeCommandHandler(
        ISpecificationAttributeService specificationAttributeService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _specificationAttributeService = specificationAttributeService;
        _translationService = translationService;
        _workContext = workContext;
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