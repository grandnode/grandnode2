using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class
    AddProductAttributeMappingCommandHandler : IRequestHandler<AddProductAttributeMappingCommand,
    ProductAttributeMappingDto>
{
    private readonly IProductAttributeService _productAttributeService;

    public AddProductAttributeMappingCommandHandler(IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
    }

    public async Task<ProductAttributeMappingDto> Handle(AddProductAttributeMappingCommand request,
        CancellationToken cancellationToken)
    {
        //insert mapping
        var productAttributeMapping = request.Model.ToEntity();
        await _productAttributeService.InsertProductAttributeMapping(productAttributeMapping, request.Product.Id);

        return productAttributeMapping.ToModel();
    }
}