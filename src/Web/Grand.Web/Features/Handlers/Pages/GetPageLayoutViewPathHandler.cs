using Grand.Business.Cms.Interfaces;
using Grand.Web.Features.Models.Pages;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var layout = await _pageLayoutService.GetPageLayoutById(request.LayoutId);
            if (layout == null)
                layout = (await _pageLayoutService.GetAllPageLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;
        }
    }
}
