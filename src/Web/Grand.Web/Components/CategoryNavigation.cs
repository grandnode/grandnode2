using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class CategoryNavigationViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;

        public CategoryNavigationViewComponent(
            IMediator mediator,
            IWorkContext workContext)
        {
            _mediator = mediator;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(string currentCategoryId, string currentProductId)
        {
            var model = await _mediator.Send(new GetCategoryNavigation() {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                CurrentCategoryId = currentCategoryId,
                CurrentProductId = currentProductId
            });
            return View(model);
        }
    }
}