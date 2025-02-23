using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CategoryNavigationViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    public CategoryNavigationViewComponent(
        IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync(string currentCategoryId, string currentProductId)
    {
        var model = await _mediator.Send(new GetCategoryNavigation {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore,
            CurrentCategoryId = currentCategoryId,
            CurrentProductId = currentProductId
        });
        return View(model);
    }
}