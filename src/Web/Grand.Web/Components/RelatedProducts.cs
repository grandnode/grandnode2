using Grand.Business.Catalog.Interfaces.Products;
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
    public class RelatedProductsViewComponent : BaseViewComponent
    {
        #region Fields
        private readonly ICacheBase _cacheBase;
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;

        private readonly CatalogSettings _catalogSettings;
        #endregion

        #region Constructors

        public RelatedProductsViewComponent(
            ICacheBase cacheBase,
            IProductService productService,
            IWorkContext workContext,
            IMediator mediator,
            CatalogSettings catalogSettings)
        {
            _cacheBase = cacheBase;
            _productService = productService;
            _workContext = workContext;
            _mediator = mediator;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(string productId, int? productThumbPictureSize)
        {
            var productIds = await _cacheBase.GetAsync(string.Format(CacheKeyConst.PRODUCTS_RELATED_IDS_KEY, productId, _workContext.CurrentStore.Id),
                  async () => (await _productService.GetProductById(productId)).RelatedProducts.OrderBy(x => x.DisplayOrder).Select(x => x.ProductId2).ToArray());

            //load products
            var products = await _productService.GetProductsByIds(productIds);

            var model = await _mediator.Send(new GetProductOverview()
            {
                PreparePictureModel = true,
                PreparePriceModel = true,
                PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                ProductThumbPictureSize = productThumbPictureSize,
                Products = products
            });

            return View(model);
        }

        #endregion
    }
}
