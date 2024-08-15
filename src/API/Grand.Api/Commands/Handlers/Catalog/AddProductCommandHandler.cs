using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class AddProductCommandHandler : IRequestHandler<AddProductCommand, ProductDto>
{
    private readonly IProductService _productService;
    private readonly ISlugService _slugService;
    private readonly ISlugNameValidator _slugNameValidator;
    public AddProductCommandHandler(
        IProductService productService,
        ISlugService slugService,
        ISlugNameValidator slugNameValidator)
    {
        _productService = productService;
        _slugService = slugService;
        _slugNameValidator = slugNameValidator;
    }

    public async Task<ProductDto> Handle(AddProductCommand request, CancellationToken cancellationToken)
    {
        var product = request.Model.ToEntity();
        await _productService.InsertProduct(product);

        request.Model.SeName = await _slugNameValidator.ValidateSeName(product, request.Model.SeName, product.Name, true);
        product.SeName = request.Model.SeName;
        //search engine name
        await _slugService.SaveSlug(product, request.Model.SeName, "");
        await _productService.UpdateProduct(product);

        return product.ToModel();
    }
}