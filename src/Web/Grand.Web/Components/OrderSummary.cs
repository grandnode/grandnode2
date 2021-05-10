using Grand.Business.Checkout.Interfaces.Orders;
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
    public class OrderSummaryViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;
        public OrderSummaryViewComponent(IMediator mediator, IShoppingCartService shoppingCartService, IWorkContext workContext)
        {
            _mediator = mediator;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync(bool? prepareAndDisplayOrderReviewData, ShoppingCartModel overriddenModel)
        {
            if (overriddenModel != null)
                return View(overriddenModel);

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentStore.Id, ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

            var model = await _mediator.Send(new GetShoppingCart()
            {
                Cart = cart,
                IsEditable = false,
                PrepareAndDisplayOrderReviewData = prepareAndDisplayOrderReviewData.GetValueOrDefault(),
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                TaxDisplayType = _workContext.TaxDisplayType
            });

            return View(model);

        }
    }
}
