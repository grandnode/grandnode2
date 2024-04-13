using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CollectionNavigationViewComponent : BaseViewComponent
{
    private readonly CatalogSettings _catalogSettings;
    private readonly IMediator _mediator;
    private readonly IWorkContext _workContext;

    public CollectionNavigationViewComponent(
        IMediator mediator,
        IWorkContext workContext,
        CatalogSettings catalogSettings)
    {
        _mediator = mediator;
        _workContext = workContext;
        _catalogSettings = catalogSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync(string currentCollectionId)
    {
        if (_catalogSettings.CollectionsBlockItemsToDisplay == 0)
            return Content("");

        var model = await _mediator.Send(new GetCollectionNavigation {
            CurrentCollectionId = currentCollectionId,
            Customer = _workContext.CurrentCustomer,
            Language = _workContext.WorkingLanguage,
            Store = _workContext.CurrentStore
        });

        return !model.Collections.Any() ? Content("") : View(model);
    }
}