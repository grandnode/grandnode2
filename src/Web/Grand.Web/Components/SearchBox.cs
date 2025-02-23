using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class SearchBoxViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    public SearchBoxViewComponent(
        IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetSearchBox {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Language = _contextAccessor.WorkContext.WorkingLanguage
        });
        return View(model);
    }
}