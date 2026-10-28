using System.Text.Json;
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
using Grand.Web.Models.Checkout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class CheckoutControllerOrderApprovalTests
{
    private Mock<IOrderApprovalService> _approval;
    private CheckoutController _controller;
    private Mock<ICustomerService> _customerService;
    private Mock<IPaymentService> _paymentService;

    [TestInitialize]
    public void Init()
    {
        var customer = new Customer { Id = "buyer", OwnerId = "owner" };
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

        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetIsPaymentWorkflowRequired>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _approval = new Mock<IOrderApprovalService>();
        _approval.Setup(a => a.RequiresApproval(cart, It.IsAny<CalculationContext>()))
            .ReturnsAsync(new OrderApprovalCheckoutState(true, 100, 500));
        _customerService = new Mock<ICustomerService>();
        _paymentService = new Mock<IPaymentService>();

        _controller = new CheckoutController(context.Object, translation.Object, _customerService.Object,
            new Mock<IGroupService>().Object, cartService.Object, new Mock<IShippingService>().Object,
            new Mock<IPickupPointService>().Object, _paymentService.Object,
            new Mock<IPaymentTransactionService>().Object, new Mock<ILogger<CheckoutController>>().Object,
            new Mock<IOrderService>().Object, mediator.Object, new Mock<IProductService>().Object,
            new Mock<IShoppingCartValidator>().Object, _approval.Object, new OrderSettings(), new OidcLoginConfig(),
            new LoyaltyPointsSettings(), new PaymentSettings(), new ShippingSettings(), new AddressSettings());
    }

    [TestMethod]
    public async Task SavePaymentMethod_ApprovalRequiredAndMethodNotAllowed_RefusesWithoutSaving()
    {
        _approval.Setup(a => a.IsPaymentMethodAllowed("Payments.Card")).Returns(false);

        var result = await _controller.SavePaymentMethod(new CheckoutPaymentMethodModel { PaymentMethod = "Payments.Card" });

        var json = JsonSerializer.Serialize(((JsonResult)result).Value);
        StringAssert.Contains(json, "\"error\":1");
        StringAssert.Contains(json, "Checkout.OrderApproval.PaymentMethodNotAllowed");
        _paymentService.Verify(p => p.LoadPaymentMethodBySystemName(It.IsAny<string>()), Times.Never);
        _customerService.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
