using Grand.Domain.News;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.News;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class HomePageNewsViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly NewsSettings _newsSettings;

    public HomePageNewsViewComponent(IMediator mediator,
        NewsSettings newsSettings)
    {
        _mediator = mediator;
        _newsSettings = newsSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_newsSettings.Enabled || !_newsSettings.ShowNewsOnMainPage)
            return Content("");

        var model = await _mediator.Send(new GetHomePageNewsItems());
        return !model.NewsItems.Any() ? Content("") : View(model);
    }
}