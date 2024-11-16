using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductAttributeMappingCommandHandler : IRequestHandler<DeleteProductAttributeMappingCommand, bool>
{
    private readonly IProductAttributeService _productAttributeService;

    public DeleteProductAttributeMappingCommandHandler(IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
    }

    public async Task<bool> Handle(DeleteProductAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        //insert mapping
        var productAttributeMapping = request.Model.ToEntity();
        await _productAttributeService.DeleteProductAttributeMapping(productAttributeMapping, request.Product.Id);

        return true;
    }
}