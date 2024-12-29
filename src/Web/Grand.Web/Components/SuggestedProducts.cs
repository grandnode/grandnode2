using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class SuggestedProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public SuggestedProductsViewComponent(
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
        if (!_catalogSettings.SuggestedProductsEnabled || _catalogSettings.SuggestedProductsNumber == 0)
            return Content("");

        var products = await _mediator.Send(new GetSuggestedProductsQuery {
            CustomerTagIds = _workContextAccessor.WorkContext.CurrentCustomer.CustomerTags.ToArray(),
            ProductsNumber = _catalogSettings.SuggestedProductsNumber
        });

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