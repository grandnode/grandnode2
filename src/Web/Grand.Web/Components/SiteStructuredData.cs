using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Common;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class SiteStructuredDataViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    public SiteStructuredDataViewComponent(IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetSiteStructuredData {
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return View(model);
    }
}
