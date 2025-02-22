using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class HomePageNewProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public HomePageNewProductsViewComponent(
        IContextAccessor contextAccessor,
        IMediator mediator,
        CatalogSettings catalogSettings)
    {
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
    {
        if (!_catalogSettings.NewProductsOnHomePage)
            return Content("");

        var products = (await _mediator.Send(new GetSearchProductsQuery {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            StoreId = _contextAccessor.StoreContext.CurrentStore.Id,
            VisibleIndividuallyOnly = true,
            MarkedAsNewOnly = true,
            OrderBy = ProductSortingEnum.CreatedOn,
            PageSize = _catalogSettings.NewProductsNumberOnHomePage
        })).products;

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

    private readonly IContextAccessor _contextAccessor;
    private readonly IMediator _mediator;
    private readonly CatalogSettings _catalogSettings;

    #endregion
}