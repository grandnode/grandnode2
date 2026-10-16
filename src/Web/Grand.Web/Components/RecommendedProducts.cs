using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Extensions;
using Grand.Web.Features.Models.Products;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class RecommendedProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public RecommendedProductsViewComponent(
        IProductService productService,
        IMediator mediator,
        CatalogSettings catalogSettings,
        IContextAccessor contextAccessor)
    {
        _productService = productService;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string productId, int? productThumbPictureSize)
    {
        var productIds = (await _productService.GetProductById(productId)).RecommendedProduct.ToArray();
        if (!productIds.Any())
            return Content("");

        var products = await _productService.GetProductsByIds(productIds, _contextAccessor.CatalogAccess());

        if (!products.Any())
            return Content("");

        var model = await _mediator.Send(new GetProductOverview {
            PreparePictureModel = true,
            PreparePriceModel = true,
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
            ProductThumbPictureSize = productThumbPictureSize,
            Products = products
        });

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly IMediator _mediator;

    private readonly CatalogSettings _catalogSettings;
    private readonly IContextAccessor _contextAccessor;

    #endregion
}