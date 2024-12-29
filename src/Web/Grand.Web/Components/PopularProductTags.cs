using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class PopularProductTagsViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IWorkContextAccessor _workContextAccessor;

    public PopularProductTagsViewComponent(IMediator mediator, IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetPopularProductTags {
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return !model.Tags.Any() ? Content("") : View(model);
    }
}