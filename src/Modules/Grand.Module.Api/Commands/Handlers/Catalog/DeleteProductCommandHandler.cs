using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Module.Api.Commands.Models.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductService _productService;

    public DeleteProductCommandHandler(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Model.Id);
        if (product != null) await _productService.DeleteProduct(product);
        return true;
    }
}