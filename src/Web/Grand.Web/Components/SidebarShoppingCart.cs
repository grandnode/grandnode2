using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.Domain.Orders;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class SidebarShoppingCartViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IPermissionService _permissionService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IWorkContext _workContext;

        public SidebarShoppingCartViewComponent(IMediator mediator,
            IPermissionService permissionService,
            ShoppingCartSettings shoppingCartSettings,
            IWorkContext workContext)
        {
            _mediator = mediator;
            _permissionService = permissionService;
            _shoppingCartSettings = shoppingCartSettings;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(MiniShoppingCartModel model)
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
                return Content("");

            if (!await _permissionService.Authorize(StandardPermission.EnableShoppingCart))
                return Content("");

            model ??= await _mediator.Send(new GetMiniShoppingCart()
            {
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }
    }
}