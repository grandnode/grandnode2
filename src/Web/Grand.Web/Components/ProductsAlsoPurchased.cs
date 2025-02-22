using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Components;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class ProductsAlsoPurchasedViewComponent : BaseViewComponent
{
    #region Constructors

    public ProductsAlsoPurchasedViewComponent(
        IProductService productService,
        IMediator mediator,
        ICacheBase cacheBase,
        IOrderReportService orderReportService,
        IContextAccessor contextAccessor,
        CatalogSettings catalogSettings
    )
    {
        _productService = productService;
        _mediator = mediator;
        _cacheBase = cacheBase;
        _orderReportService = orderReportService;
        _contextAccessor = contextAccessor;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string productId, int? productThumbPictureSize)
    {
        if (!_catalogSettings.ProductsAlsoPurchasedEnabled)
            return Content("");

        //load and cache report
        var productIds = await _cacheBase.GetAsync(
            string.Format(CacheKeyConst.PRODUCTS_ALSO_PURCHASED_IDS_KEY, productId, _contextAccessor.StoreContext.CurrentStore.Id),
            () =>
                _orderReportService
                    .GetAlsoPurchasedProductsIds(_contextAccessor.StoreContext.CurrentStore.Id, productId,
                        _catalogSettings.ProductsAlsoPurchasedNumber)
        );

        //load products
        var products = await _productService.GetProductsByIds(productIds);

        if (!products.Any())
            return Content("");

        //prepare model
        var model = await _mediator.Send(new GetProductOverview {
            PreparePictureModel = true,
            PreparePriceModel = true,
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
    private readonly IOrderReportService _orderReportService;
    private readonly IContextAccessor _contextAccessor;
    private readonly CatalogSettings _catalogSettings;

    #endregion
}