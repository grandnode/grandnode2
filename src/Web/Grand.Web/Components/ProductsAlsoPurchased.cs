using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.System.Interfaces.Reports;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Domain.Catalog;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using Grand.Web.Events.Cache;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class ProductsAlsoPurchasedViewComponent : BaseViewComponent
    {
        #region Fields
        private readonly IProductService _productService;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly IOrderReportService _orderReportService;
        private readonly IWorkContext _workContext;
        private readonly CatalogSettings _catalogSettings;
        #endregion

        #region Constructors

        public ProductsAlsoPurchasedViewComponent(
            IProductService productService,
            IMediator mediator,
            ICacheBase cacheBase,
            IOrderReportService orderReportService,
            IWorkContext workContext,
            CatalogSettings catalogSettings
)
        {
            _productService = productService;
            _mediator = mediator;
            _cacheBase = cacheBase;
            _orderReportService = orderReportService;
            _workContext = workContext;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(string productId, int? productThumbPictureSize)
        {
            if (!_catalogSettings.ProductsAlsoPurchasedEnabled)
                return Content("");

            //load and cache report
            var productIds = await _cacheBase.GetAsync(string.Format(CacheKeyConst.PRODUCTS_ALSO_PURCHASED_IDS_KEY, productId, _workContext.CurrentStore.Id),
                () =>
                    _orderReportService
                    .GetAlsoPurchasedProductsIds(_workContext.CurrentStore.Id, productId, _catalogSettings.ProductsAlsoPurchasedNumber)
                    );

            //load products
            var products = await _productService.GetProductsByIds(productIds);

            if (!products.Any())
                return Content("");

            //prepare model
            var model = await _mediator.Send(new GetProductOverview()
            {
                PreparePictureModel = true,
                PreparePriceModel = true,
                ProductThumbPictureSize = productThumbPictureSize,
                Products = products,
            });

            return View(model);
        }

        #endregion

    }
}
