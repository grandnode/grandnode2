using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class
    UpdateProductAttributeMappingCommandHandler : IRequestHandler<UpdateProductAttributeMappingCommand,
    ProductAttributeMappingDto>
{
    private readonly IProductAttributeService _productAttributeService;

    public UpdateProductAttributeMappingCommandHandler(IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
    }

    public async Task<ProductAttributeMappingDto> Handle(UpdateProductAttributeMappingCommand request,
        CancellationToken cancellationToken)
    {
        //insert mapping
        var productAttributeMapping = request.Model.ToEntity();
        await _productAttributeService.UpdateProductAttributeMapping(productAttributeMapping, request.Product.Id, true);

        return productAttributeMapping.ToModel();
    }
}