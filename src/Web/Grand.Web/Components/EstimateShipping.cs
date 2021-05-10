using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Infrastructure;
using Grand.Domain.Orders;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class EstimateShippingViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;

        public EstimateShippingViewComponent(IMediator mediator, IShoppingCartService shoppingCartService, IWorkContext workContext)
        {
            _mediator = mediator;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.ShoppingCart);

            var model = await _mediator.Send(new GetEstimateShipping()
            {
                Cart = cart,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            if (!model.Enabled)
                return Content("");

            return View(model);
        }

    }
}
