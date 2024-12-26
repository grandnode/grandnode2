using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductService _productService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public DeleteProductCommandHandler(
        IProductService productService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _productService = productService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductById(request.Model.Id);
        if (product != null) await _productService.DeleteProduct(product);
        return true;
    }
}