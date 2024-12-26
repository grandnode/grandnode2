using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class ProductReviewsViewComponent : BaseViewComponent
{
    #region Constructors

    public ProductReviewsViewComponent(
        IProductService productService,
        IMediator mediator,
        IWorkContextAccessor workContextAccessor,
        CatalogSettings catalogSettings)
    {
        _productService = productService;
        _workContextAccessor = workContextAccessor;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product is not { Published: true } || !product.AllowCustomerReviews)
            return Content("");

        var model = await _mediator.Send(new GetProductReviews {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Product = product,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Size = _catalogSettings.NumberOfReview
        });

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IProductService _productService;
    private readonly IMediator _mediator;
    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly CatalogSettings _catalogSettings;

    #endregion
}