using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCategoryLayoutViewPathHandler : IRequestHandler<GetCategoryLayoutViewPath, string>
    {
        private readonly ICategoryLayoutService _categoryLayoutService;

        public GetCategoryLayoutViewPathHandler(
            ICategoryLayoutService categoryLayoutService)
        {
            _categoryLayoutService = categoryLayoutService;
        }

        public async Task<string> Handle(GetCategoryLayoutViewPath request, CancellationToken cancellationToken)
        {
            var layout = await _categoryLayoutService.GetCategoryLayoutById(request.LayoutId);
            if (layout == null)
                layout = (await _categoryLayoutService.GetAllCategoryLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;
        }
    }
}
