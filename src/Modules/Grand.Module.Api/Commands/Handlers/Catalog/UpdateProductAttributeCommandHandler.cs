using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateProductAttributeCommandHandler : IRequestHandler<UpdateProductAttributeCommand, ProductAttributeDto>
{
    private readonly IProductAttributeService _productAttributeService;

    public UpdateProductAttributeCommandHandler(IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
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