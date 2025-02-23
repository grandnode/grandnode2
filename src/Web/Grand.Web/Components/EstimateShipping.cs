using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.ShoppingCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class EstimateShippingViewComponent : BaseViewComponent
{
    private readonly IMediator _mediator;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly IContextAccessor _contextAccessor;

    public EstimateShippingViewComponent(IMediator mediator, IShoppingCartService shoppingCartService,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _shoppingCartService = shoppingCartService;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await _shoppingCartService.GetShoppingCart(_contextAccessor.StoreContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart);

        var model = await _mediator.Send(new GetEstimateShipping {
            Cart = cart,
            Currency = _contextAccessor.WorkContext.WorkingCurrency,
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return !model.Enabled ? Content("") : View(model);
    }
}