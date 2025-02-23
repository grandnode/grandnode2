using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductAttributeCommandHandler : IRequestHandler<DeleteProductAttributeCommand, bool>
{
    private readonly IProductAttributeService _productAttributeService;

    public DeleteProductAttributeCommandHandler(IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
    }

    public async Task<bool> Handle(DeleteProductAttributeCommand request, CancellationToken cancellationToken)
    {
        var productAttribute = await _productAttributeService.GetProductAttributeById(request.Model.Id);
        if (productAttribute != null) await _productAttributeService.DeleteProductAttribute(productAttribute);
        return true;
    }
}