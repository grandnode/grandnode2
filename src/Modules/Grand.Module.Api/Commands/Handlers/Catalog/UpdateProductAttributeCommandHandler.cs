using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductAttributeCommandHandler : IRequestHandler<UpdateProductAttributeCommand, ProductAttributeDto>
{
    private readonly IProductAttributeService _productAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public UpdateProductAttributeCommandHandler(
        IProductAttributeService productAttributeService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _productAttributeService = productAttributeService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<ProductAttributeDto> Handle(UpdateProductAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var productAttribute = await _productAttributeService.GetProductAttributeById(request.Model.Id);

        productAttribute = request.Model.ToEntity(productAttribute);
        await _productAttributeService.UpdateProductAttribute(productAttribute);

        return productAttribute.ToModel();
    }
}