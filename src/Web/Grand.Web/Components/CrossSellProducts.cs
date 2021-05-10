using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class CrossSellProductsViewComponent : BaseViewComponent
    {
        #region Fields
        private readonly IProductService _productService;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;

        private readonly CatalogSettings _catalogSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        #endregion

        #region Constructors

        public CrossSellProductsViewComponent(
            IProductService productService,
            IWorkContext workContext,
            IMediator mediator,
            CatalogSettings catalogSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _productService = productService;
            _workContext = workContext;
            _mediator = mediator;
            _catalogSettings = catalogSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
        {
            if (_shoppingCartSettings.CrossSellsNumber == 0)
                return Content("");

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContext.CurrentStore.Id)
                .ToList();

            var products = await _productService.GetCrossSellProductsByShoppingCart(cart, _shoppingCartSettings.CrossSellsNumber);

            if (!products.Any())
                return Content("");

            var model = await _mediator.Send(new GetProductOverview()
            {
                PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                ProductThumbPictureSize = productThumbPictureSize,
                Products = products,
                ForceRedirectionAfterAddingToCart = true,
            });

            return View(model);

        }

        #endregion

    }
}
