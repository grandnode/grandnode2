using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddProductAttributeCommandHandler : IRequestHandler<AddProductAttributeCommand, ProductAttributeDto>
{
    private readonly IProductAttributeService _productAttributeService;

    public AddProductAttributeCommandHandler(
        IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
    }

    public async Task<ProductAttributeDto> Handle(AddProductAttributeCommand request,
        CancellationToken cancellationToken)
    {
        var productAttribute = request.Model.ToEntity();
        await _productAttributeService.InsertProductAttribute(productAttribute);

        return productAttribute.ToModel();
    }
}