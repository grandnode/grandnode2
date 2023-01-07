﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Web.Common.Components;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components
{
    public class HomePageProductsViewComponent : BaseViewComponent
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Constructors
        public HomePageProductsViewComponent(
            IProductService productService,
            IMediator mediator,
            ICacheBase cacheBase,
            CatalogSettings catalogSettings)
        {
            _productService = productService;
            _mediator = mediator;
            _cacheBase = cacheBase;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
        {
            var productsIds = await _cacheBase.GetAsync(CacheKeyConst.HOMEPAGE_PRODUCTS_MODEL_KEY,
                    async () => await _productService.GetAllProductsDisplayedOnHomePage());

            var products = await _productService.GetProductsByIds(productsIds.ToArray());

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

    }
}
