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
    public class OrderTotalsViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;

        public OrderTotalsViewComponent(
            IMediator mediator,
            IShoppingCartService shoppingCartService,
            IWorkContext workContext)
        {
            _mediator = mediator;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool isEditable)
        {
            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

            var model = await _mediator.Send(new GetOrderTotals()
            {
                Cart = cart,
                IsEditable = isEditable,
                Store = _workContext.CurrentStore,
                Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                TaxDisplayType = _workContext.TaxDisplayType
            });
            return View(model);
        }
    }
}
