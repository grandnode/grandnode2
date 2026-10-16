using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Components;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Products;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class HomePageProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public HomePageProductsViewComponent(
        IProductService productService,
        IMediator mediator,
        ICacheBase cacheBase,
        CatalogSettings catalogSettings,
        IContextAccessor contextAccessor)
    {
        _productService = productService;
        _mediator = mediator;
        _cacheBase = cacheBase;
        _catalogSettings = catalogSettings;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
    {
        var productsIds = await _cacheBase.GetAsync(CacheKeyConst.HOMEPAGE_PRODUCTS_MODEL_KEY,
            async () => await _productService.GetAllProductsDisplayedOnHomePage());

        var products = await _productService.GetProductsByIds(productsIds.ToArray(), _contextAccessor.WorkContext.CurrentCustomer,
            _contextAccessor.StoreContext.CurrentStore.Id);

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
    private readonly ICacheBase _cacheBase;
    private readonly CatalogSettings _catalogSettings;
    private readonly IContextAccessor _contextAccessor;

    #endregion
}