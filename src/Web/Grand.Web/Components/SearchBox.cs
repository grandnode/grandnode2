using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.ViewComponents
{
    public class SearchBoxViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;

        public SearchBoxViewComponent(
            IMediator mediator,
            IWorkContext workContext)
        {
            _mediator = mediator;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await _mediator.Send(new GetSearchBox() {
                Customer = _workContext.CurrentCustomer,
                Store = _workContext.CurrentStore,
                Language = _workContext.WorkingLanguage
            });
            return View(model);
        }
    }
}