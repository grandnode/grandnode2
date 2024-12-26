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
        IWorkContextAccessor workContextAccessor,
        IMediator mediator,
        CatalogSettings catalogSettings)
    {
        _workContextAccessor = workContextAccessor;
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
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            StoreId = _workContextAccessor.WorkContext.CurrentStore.Id,
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

    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IMediator _mediator;
    private readonly CatalogSettings _catalogSettings;

    #endregion
}