using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class HomePageCollectionsViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    public HomePageCollectionsViewComponent(
        IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetHomepageCollections {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        return !model.Any() ? Content("") : View(model);
    }
}