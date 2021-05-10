using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetBrandLayoutViewPathHandler : IRequestHandler<GetBrandLayoutViewPath, string>
    {
        private readonly IBrandLayoutService _brandLayoutService;

        public GetBrandLayoutViewPathHandler(
            IBrandLayoutService brandLayoutService)
        {
            _brandLayoutService = brandLayoutService;
        }

        public async Task<string> Handle(GetBrandLayoutViewPath request, CancellationToken cancellationToken)
        {
            var layout = await _brandLayoutService.GetBrandLayoutById(request.LayoutId);
            if (layout == null)
                layout = (await _brandLayoutService.GetAllBrandLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;
        }
    }
}
