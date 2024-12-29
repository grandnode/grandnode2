using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddProductAttributeCommandHandler : IRequestHandler<AddProductAttributeCommand, ProductAttributeDto>
{
    private readonly IProductAttributeService _productAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public AddProductAttributeCommandHandler(
        IProductAttributeService productAttributeService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _productAttributeService = productAttributeService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<ProductAttributeDto> Handle(AddProductAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var productAttribute = request.Model.ToEntity();
        await _productAttributeService.InsertProductAttribute(productAttribute);

        return productAttribute.ToModel();
    }
}