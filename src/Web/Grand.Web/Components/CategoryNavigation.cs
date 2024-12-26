using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CategoryNavigationViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IWorkContextAccessor _workContextAccessor;

    public CategoryNavigationViewComponent(
        IMediator mediator,
        IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync(string currentCategoryId, string currentProductId)
    {
        var model = await _mediator.Send(new GetCategoryNavigation {
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore,
            CurrentCategoryId = currentCategoryId,
            CurrentProductId = currentProductId
        });
        return View(model);
    }
}