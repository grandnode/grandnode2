using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteProductAttributeCommandHandler : IRequestHandler<DeleteProductAttributeCommand, bool>
{
    private readonly IProductAttributeService _productAttributeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public DeleteProductAttributeCommandHandler(
        IProductAttributeService productAttributeService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _productAttributeService = productAttributeService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<bool> Handle(DeleteProductAttributeCommand request, CancellationToken cancellationToken)
    {
        var productAttribute = await _productAttributeService.GetProductAttributeById(request.Model.Id);
        if (productAttribute != null) await _productAttributeService.DeleteProductAttribute(productAttribute);
        return true;
    }
}