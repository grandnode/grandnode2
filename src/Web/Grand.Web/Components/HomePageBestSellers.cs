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
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Grand.Web.Components
{
    public class HomePageBestSellersViewComponent : BaseViewComponent
    {
        #region Fields
        private readonly IOrderReportService _orderReportService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly IMediator _mediator;

        private readonly CatalogSettings _catalogSettings;
        #endregion

        #region Constructors

        public HomePageBestSellersViewComponent(
            IOrderReportService orderReportService,
            ICacheBase cacheBase,
            IWorkContext workContext,
            IProductService productService,
            IMediator mediator,
            CatalogSettings catalogSettings)
        {
            _orderReportService = orderReportService;
            _cacheBase = cacheBase;
            _workContext = workContext;
            _productService = productService;
            _mediator = mediator;
            _catalogSettings = catalogSettings;
        }


        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
        {
            if (!_catalogSettings.ShowBestsellersOnHomepage || _catalogSettings.NumberOfBestsellersOnHomepage == 0)
                return Content("");

            var productIds = new List<string>();

            //load and cache report
            if (_catalogSettings.BestsellersFromReports)
            {
                var fromdate = DateTime.UtcNow.AddMonths(_catalogSettings.PeriodBestsellers > 0 ? -_catalogSettings.PeriodBestsellers : -12);
                var report = await _cacheBase.GetAsync(string.Format(CacheKeyConst.HOMEPAGE_BESTSELLERS_IDS_KEY, _workContext.CurrentStore.Id), async () =>
                                    await _orderReportService.BestSellersReport(
                                        createdFromUtc: fromdate,
                                        ps: Domain.Payments.PaymentStatus.Paid,
                                        storeId: _workContext.CurrentStore.Id,
                                        pageSize: _catalogSettings.NumberOfBestsellersOnHomepage));

                productIds = report.Select(x => x.ProductId).ToList();
            }
            else
            {
                productIds = await _cacheBase.GetAsync(CacheKeyConst.BESTSELLER_PRODUCTS_MODEL_KEY,
                    async () => (await _productService.GetAllProductsDisplayedOnBestSeller()).ToList());

            }
            //load products
            var products = await _productService.GetProductsByIds(productIds.ToArray());

            if (!products.Any())
                return Content("");

            var model = await _mediator.Send(new GetProductOverview()
            {
                ProductThumbPictureSize = productThumbPictureSize,
                Products = products.Take(_catalogSettings.NumberOfBestsellersOnHomepage),
            });

            return View(model);
        }

        #endregion

    }
}
