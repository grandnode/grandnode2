using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class PopularProductTagsViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IWorkContext _workContext;

    public PopularProductTagsViewComponent(IMediator mediator, IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _workContext = workContextAccessor.WorkContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetPopularProductTags {
            Language = _workContext.WorkingLanguage,
            Store = _workContext.CurrentStore
        });
        return !model.Tags.Any() ? Content("") : View(model);
    }
}