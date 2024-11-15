using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IProductService _productService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    public AddProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        ISeNameService seNameService)
    {
        _productService = productService;
        _slugService = slugService;
        _seNameService = seNameService;
    }

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = request.Model.ToEntity();
        await _productService.InsertProduct(product);

        request.Model.SeName = await _seNameService.ValidateSeName(product, request.Model.SeName, product.Name, true);
        product.SeName = request.Model.SeName;
        //search engine name
        await _slugService.SaveSlug(product, request.Model.SeName, "");
        await _productService.UpdateProduct(product);

        return product.ToModel();
    }
}