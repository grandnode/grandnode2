using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class PopularProductTagsViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;

        public PopularProductTagsViewComponent(IMediator mediator, IWorkContext workContext)
        {
            _mediator = mediator;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await _mediator.Send(new GetPopularProductTags() {
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            if (!model.Tags.Any())
                return Content("");

            return View(model);

        }
    }
}