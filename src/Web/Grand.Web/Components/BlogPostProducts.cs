using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Common.Extensions;
using Grand.Web.Features.Models.Products;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class BlogPostProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public BlogPostProductsViewComponent(
        IProductService productService,
        IMediator mediator,
        IBlogService blogService,
        CatalogSettings catalogSettings,
        IContextAccessor contextAccessor)
    {
        _productService = productService;
        _mediator = mediator;
        _blogService = blogService;
        _catalogSettings = catalogSettings;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string blogPostId, int? productThumbPictureSize)
    {
        var blogproducts = await _blogService.GetProductsByBlogPostId(blogPostId);
        if (!blogproducts.Any())
            return Content("");

        var products = await _productService.GetProductsByIds(blogproducts.Select(x => x.ProductId).ToArray(),
            _contextAccessor.CatalogAccess());

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
    private readonly IBlogService _blogService;

    private readonly CatalogSettings _catalogSettings;
    private readonly IContextAccessor _contextAccessor;

    #endregion
}