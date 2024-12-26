using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CrossSellProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public CrossSellProductsViewComponent(
        IProductService productService,
        IWorkContextAccessor workContextAccessor,
        IMediator mediator,
        CatalogSettings catalogSettings,
        ShoppingCartSettings shoppingCartSettings)
    {
        _productService = productService;
        _workContextAccessor = workContextAccessor;
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

        var cart = _workContextAccessor.WorkContext.CurrentCustomer.ShoppingCartItems
            .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, _workContextAccessor.WorkContext.CurrentStore.Id)
            .ToList();

        var products =
            await _productService.GetCrossSellProductsByShoppingCart(cart, _shoppingCartSettings.CrossSellsNumber);

        if (!products.Any())
            return Content("");

        var model = await _mediator.Send(new GetProductOverview {
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
            ProductThumbPictureSize = productThumbPictureSize,
            Products = products,
            ForceRedirectionAfterAddingToCart = true
        });

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IMediator _mediator;

    private readonly CatalogSettings _catalogSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    #endregion
}