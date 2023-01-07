﻿using Grand.Business.Core.Interfaces.Cms;
using Grand.Web.Features.Models.Pages;
using MediatR;

namespace Grand.Web.Features.Handlers.Pages
{
    public class GetPageLayoutViewPathHandler : IRequestHandler<GetPageLayoutViewPath, string>
    {
        private readonly IPageLayoutService _pageLayoutService;

        public GetPageLayoutViewPathHandler(
            IPageLayoutService pageLayoutService)
        {
            _pageLayoutService = pageLayoutService;
        }

        public async Task<string> Handle(GetPageLayoutViewPath request, CancellationToken cancellationToken)
        {
            var layout = await _pageLayoutService.GetPageLayoutById(request.LayoutId) ?? (await _pageLayoutService.GetAllPageLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;
        }
    }
}
