﻿using Grand.Domain.Blogs;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Blogs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components
{
    public class BlogTagsViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly BlogSettings _blogSettings;

        public BlogTagsViewComponent(IMediator mediator, BlogSettings blogSettings)
        {
            _mediator = mediator;
            _blogSettings = blogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_blogSettings.Enabled)
                return Content("");

            var model = await _mediator.Send(new GetBlogPostTagList());
            return View(model);

        }
    }
}