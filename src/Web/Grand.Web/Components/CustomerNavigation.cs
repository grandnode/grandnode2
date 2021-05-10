using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Customers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class CustomerNavigationViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;

        public CustomerNavigationViewComponent(IMediator mediator,
            IWorkContext workContext)
        {
            _mediator = mediator;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(int selectedTabId = 0)
        {
            var model = await _mediator.Send(new GetNavigation() {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                SelectedTabId = selectedTabId,
                Store = _workContext.CurrentStore,
                Vendor = _workContext.CurrentVendor
            });
            return View(model);
        }
    }
}
