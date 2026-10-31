using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Checkout;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class CheckoutControllerConfirmOrderTests
{
    private CheckoutController _controller;
    private Mock<IMediator> _mediator;

    [TestInitialize]
    public void Init()
    {
        var customer = new Customer { Id = "buyer" };
        var store = new Grand.Domain.Stores.Store { Id = "s1" };
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(customer);
        work.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "en" });
        work.Setup(w => w.WorkingCurrency).Returns(new Currency { Id = "c1", CurrencyCode = "EUR" });
        work.Setup(w => w.TaxDisplayType).Returns(TaxDisplayType.IncludingTax);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(s => s.CurrentStore).Returns(store);
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(storeContext.Object);

        var cart = new List<ShoppingCartItem> { new() { ProductId = "p1", Quantity = 1 } };
        var cartService = new Mock<IShoppingCartService>();
        cartService.Setup(s => s.GetShoppingCart(customer, "s1", It.IsAny<ShoppingCartType[]>()))
            .ReturnsAsync(cart);

        _mediator = new Mock<IMediator>();
        _mediator.Setup(m => m.Send(It.IsAny<GetMinOrderPlaceIntervalValid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mediator.Setup(m => m.Send(It.IsAny<PlaceOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaceOrderResult {
                PaymentTransaction = new PaymentTransaction { PaymentMethodSystemName = "Payments.OnAccount" }
            });

        _controller = new CheckoutController(context.Object, new Mock<IRequestTranslationService>().Object,
            new Mock<ICustomerService>().Object, new Mock<IGroupService>().Object, cartService.Object,
            new Mock<IShippingService>().Object, new Mock<IPickupPointService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<ILogger<CheckoutController>>().Object, new Mock<IOrderService>().Object, _mediator.Object,
            new Mock<IProductService>().Object, new Mock<IShoppingCartValidator>().Object,
            new Mock<IOrderApprovalService>().Object, new Mock<IOrderPlacementService>().Object, new OrderSettings(),
            new OidcLoginConfig(),
            new LoyaltyPointsSettings(), new PaymentSettings(), new ShippingSettings(), new AddressSettings()) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestMethod]
    public async Task ConfirmOrder_PassesPurchaseOrderNumberToCommand()
    {
        await _controller.ConfirmOrder("PO-77");

        _mediator.Verify(m => m.Send(It.Is<PlaceOrderCommand>(c => c.PurchaseOrderNumber == "PO-77"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
