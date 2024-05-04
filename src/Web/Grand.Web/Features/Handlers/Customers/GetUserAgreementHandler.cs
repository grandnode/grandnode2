using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers;

public class GetUserAgreementHandler : IRequestHandler<GetUserAgreement, UserAgreementModel>
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;

    public GetUserAgreementHandler(IOrderService orderService,
        IProductService productService)
    {
        _orderService = orderService;
        _productService = productService;
    }

    public async Task<UserAgreementModel> Handle(GetUserAgreement request, CancellationToken cancellationToken)
    {
        var orderItem = await _orderService.GetOrderItemByGuid(request.OrderItemId);
        if (orderItem == null)
            return null;

        var product = await _productService.GetProductById(orderItem.ProductId);
        if (product is not { HasUserAgreement: true })
            return null;

        var model = new UserAgreementModel {
            UserAgreementText = product.UserAgreementText,
            OrderItemGuid = request.OrderItemId
        };
        return model;
    }
}