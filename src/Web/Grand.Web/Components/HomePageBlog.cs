using Grand.Domain.Blogs;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Blogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class HomePageBlogViewComponent : BaseViewComponent
{
    private readonly BlogSettings _blogSettings;
    private readonly IMediator _mediator;

    public HomePageBlogViewComponent(IMediator mediator,
        BlogSettings blogSettings)
    {
        _mediator = mediator;
        _blogSettings = blogSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!_blogSettings.Enabled || !_blogSettings.ShowBlogOnHomePage)
            return Content("");

        var model = await _mediator.Send(new GetHomePageBlog());
        return View(model);
    }
}