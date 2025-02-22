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
    private readonly IContextAccessor _contextAccessor;

    public CollectionNavigationViewComponent(
        IMediator mediator,
        IContextAccessor contextAccessor,
        CatalogSettings catalogSettings)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
        _catalogSettings = catalogSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync(string currentCollectionId)
    {
        if (_catalogSettings.CollectionsBlockItemsToDisplay == 0)
            return Content("");

        var model = await _mediator.Send(new GetCollectionNavigation {
            CurrentCollectionId = currentCollectionId,
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        return !model.Collections.Any() ? Content("") : View(model);
    }
}