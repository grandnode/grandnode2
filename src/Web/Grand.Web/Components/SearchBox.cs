using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class SearchBoxViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IWorkContextAccessor _workContextAccessor;

    public SearchBoxViewComponent(
        IMediator mediator,
        IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetSearchBox {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            Language = _workContextAccessor.WorkContext.WorkingLanguage
        });
        return View(model);
    }
}