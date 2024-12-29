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
    private readonly IWorkContextAccessor _workContextAccessor;

    public EstimateShippingViewComponent(IMediator mediator, IShoppingCartService shoppingCartService,
        IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _shoppingCartService = shoppingCartService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await _shoppingCartService.GetShoppingCart(_workContextAccessor.WorkContext.CurrentStore.Id,
            ShoppingCartType.ShoppingCart);

        var model = await _mediator.Send(new GetEstimateShipping {
            Cart = cart,
            Currency = _workContextAccessor.WorkContext.WorkingCurrency,
            Customer = _workContextAccessor.WorkContext.CurrentCustomer,
            Language = _workContextAccessor.WorkContext.WorkingLanguage,
            Store = _workContextAccessor.WorkContext.CurrentStore
        });
        return !model.Enabled ? Content("") : View(model);
    }
}