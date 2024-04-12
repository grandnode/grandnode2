using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class UpdateProductAttributeCommandHandler : IRequestHandler<UpdateProductAttributeCommand, ProductAttributeDto>
{
    private readonly IProductAttributeService _productAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public UpdateProductAttributeCommandHandler(
        IProductAttributeService productAttributeService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _productAttributeService = productAttributeService;
        _translationService = translationService;
        _workContext = workContext;
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