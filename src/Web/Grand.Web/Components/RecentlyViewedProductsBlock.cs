using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class RecentlyViewedProductsBlockViewComponent : BaseViewComponent
{
    #region Constructors

    public RecentlyViewedProductsBlockViewComponent(
        IWorkContextAccessor workContextAccessor,
        IRecentlyViewedProductsService recentlyViewedProductsService,
        IMediator mediator,
        CatalogSettings catalogSettings)
    {
        _workContextAccessor = workContextAccessor;
        _recentlyViewedProductsService = recentlyViewedProductsService;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize, bool? preparePriceModel)
    {
        if (!_catalogSettings.RecentlyViewedProductsEnabled)
            return Content("");

        var preparePictureModel = productThumbPictureSize.HasValue;
        var products = await _recentlyViewedProductsService.GetRecentlyViewedProducts(_workContextAccessor.WorkContext.CurrentCustomer.Id,
            _catalogSettings.RecentlyViewedProductsNumber);

        if (!products.Any())
            return Content("");

        var model = await _mediator.Send(new GetProductOverview {
            PreparePictureModel = preparePictureModel,
            PreparePriceModel = preparePriceModel.GetValueOrDefault(),
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
            ProductThumbPictureSize = productThumbPictureSize,
            Products = products
        });

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IWorkContextAccessor _workContextAccessor;
    private readonly IRecentlyViewedProductsService _recentlyViewedProductsService;
    private readonly IMediator _mediator;

    private readonly CatalogSettings _catalogSettings;

    #endregion
}