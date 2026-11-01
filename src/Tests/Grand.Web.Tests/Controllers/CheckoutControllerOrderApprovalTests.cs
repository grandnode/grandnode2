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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class CheckoutControllerOrderApprovalTests
{
    private Mock<IOrderApprovalService> _approval;
    private List<ShoppingCartItem> _cart;
    private CheckoutController _controller;
    private Mock<ICustomerService> _customerService;
    private Language _language;
    private Mock<IMediator> _mediator;
    private Mock<IPaymentService> _paymentService;
    private PaymentSettings _paymentSettings;
    private Mock<IOrderPlacementService> _placement;
    private Mock<IShippingService> _shippingService;

    [TestInitialize]
    public void Init()
    {
        var customer = new Customer { Id = "buyer", OwnerId = "owner" };
        var store = new Grand.Domain.Stores.Store { Id = "s1" };
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(customer);
        var language = new Language { Id = "en" };
        _cart = new List<ShoppingCartItem> { new() { ProductId = "p1", Quantity = 1 } };
        _language = language;
        work.Setup(w => w.WorkingLanguage).Returns(language);
        work.Setup(w => w.WorkingCurrency).Returns(new Currency { Id = "c1", CurrencyCode = "EUR" });
        work.Setup(w => w.TaxDisplayType).Returns(TaxDisplayType.IncludingTax);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(s => s.CurrentStore).Returns(store);
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(storeContext.Object);

        var cart = _cart;
        var cartService = new Mock<IShoppingCartService>();
        cartService.Setup(s => s.GetShoppingCart(customer, "s1", It.IsAny<ShoppingCartType[]>()))
            .ReturnsAsync(cart);

        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);

        _mediator = new Mock<IMediator>();
        _mediator.Setup(m => m.Send(It.IsAny<GetIsPaymentWorkflowRequired>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _approval = new Mock<IOrderApprovalService>();
        _placement = new Mock<IOrderPlacementService>();
        _placement.Setup(p => p.Evaluate(cart, It.IsAny<CalculationContext>(), language, "Payments.Card",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPlacementEvaluation(OrderPlacementDecision.RequireApproval, ["over the limit"]));
        _customerService = new Mock<ICustomerService>();
        _paymentService = new Mock<IPaymentService>();
        _shippingService = new Mock<IShippingService>();
        _paymentSettings = new PaymentSettings();

        _controller = new CheckoutController(context.Object, translation.Object, _customerService.Object,
            new Mock<IGroupService>().Object, cartService.Object, _shippingService.Object,
            new Mock<IPickupPointService>().Object, _paymentService.Object,
            new Mock<IPaymentTransactionService>().Object, new Mock<ILogger<CheckoutController>>().Object,
            new Mock<IOrderService>().Object, _mediator.Object, new Mock<IProductService>().Object,
            new Mock<IShoppingCartValidator>().Object, _approval.Object, _placement.Object, new OrderSettings(),
            new OidcLoginConfig(),
            new LoyaltyPointsSettings(), _paymentSettings, new ShippingSettings(), new AddressSettings()) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
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

    [TestMethod]
    public async Task SavePaymentMethod_Blocked_ReturnsReasons()
    {
        Blocks("blocked customer", "banned product");

        var result = await _controller.SavePaymentMethod(new CheckoutPaymentMethodModel { PaymentMethod = "Payments.Card" });

        var json = JsonSerializer.Serialize(((JsonResult)result).Value);
        StringAssert.Contains(json, "\"error\":1");
        StringAssert.Contains(json, "blocked customer banned product");
        _paymentService.Verify(p => p.LoadPaymentMethodBySystemName(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task SavePaymentMethod_PassesTheRequestTokenToTheRules()
    {
        using var source = new CancellationTokenSource();
        _controller.ControllerContext.HttpContext.RequestAborted = source.Token;

        await _controller.SavePaymentMethod(new CheckoutPaymentMethodModel { PaymentMethod = "Payments.Card" });

        _placement.Verify(p => p.Evaluate(_cart, It.IsAny<CalculationContext>(), _language, "Payments.Card",
            source.Token), Times.Once);
    }

    private void Blocks(params string[] reasons)
    {
        _placement.Setup(p => p.Evaluate(_cart, It.IsAny<CalculationContext>(), _language, "Payments.Card",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPlacementEvaluation(OrderPlacementDecision.Block, reasons));
    }

    /// <summary>
    ///     Saves the shipping method of a checkout with one payment method, whose rules get to answer for it
    /// </summary>
    private async Task<string> SaveShippingMethodWithOnlyTheCardToPayWith()
    {
        _paymentSettings.SkipPaymentIfOnlyOne = true;
        _paymentSettings.ActivePaymentProviderSystemNames = ["Payments.Card"];
        _cart[0].IsShipEnabled = true;

        var shippingProvider = new Mock<IShippingRateCalculationProvider>();
        shippingProvider.Setup(p => p.ValidateShippingForm(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
            .ReturnsAsync(new List<string>());
        _shippingService.Setup(s => s.LoadShippingRateCalculationProviderBySystemName("Flat"))
            .Returns(shippingProvider.Object);
        _shippingService.Setup(s => s.GetShippingOptions(It.IsAny<Customer>(), _cart, It.IsAny<Address>(), "Flat",
                It.IsAny<Grand.Domain.Stores.Store>()))
            .ReturnsAsync(new GetShippingOptionResponse { ShippingOptions = { new ShippingOption { Name = "Courier" } } });

        var paymentStep = new CheckoutPaymentMethodModel();
        paymentStep.PaymentMethods.Add(new CheckoutPaymentMethodModel.PaymentMethodModel {
            PaymentMethodSystemName = "Payments.Card"
        });
        _mediator.Setup(m => m.Send(It.IsAny<GetPaymentMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentStep);

        var provider = new Mock<IPaymentProvider>();
        provider.Setup(p => p.SystemName).Returns("Payments.Card");
        provider.Setup(p => p.LimitedToStores).Returns([]);
        provider.Setup(p => p.SkipPaymentInfo()).ReturnsAsync(true);
        _paymentService.Setup(p => p.LoadPaymentMethodBySystemName("Payments.Card")).Returns(provider.Object);

        var result = await _controller.SaveShippingMethod(new CheckoutShippingMethodModel {
            ShippingOption = "Courier:Flat"
        });
        return JsonSerializer.Serialize(((JsonResult)result).Value);
    }

    private void CardWasNotSelected()
    {
        _customerService.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), SystemCustomerFieldNames.SelectedPaymentMethod,
            "Payments.Card", It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OnlyPaymentMethod_ApprovalRequiredAndNotAllowed_ShowsThePaymentStepInsteadOfADeadEnd()
    {
        _approval.Setup(a => a.IsPaymentMethodAllowed("Payments.Card")).Returns(false);

        var json = await SaveShippingMethodWithOnlyTheCardToPayWith();

        StringAssert.Contains(json, "\"goto_section\":\"payment_method\"");
        CardWasNotSelected();
    }

    [TestMethod]
    public async Task OnlyPaymentMethod_BlockedForThatMethod_ShowsThePaymentStepInsteadOfADeadEnd()
    {
        Blocks("not with this method");

        var json = await SaveShippingMethodWithOnlyTheCardToPayWith();

        StringAssert.Contains(json, "\"goto_section\":\"payment_method\"");
        CardWasNotSelected();
    }

    [TestMethod]
    public async Task OnlyPaymentMethod_Accepted_IsSelectedAndTheStepSkipped()
    {
        _approval.Setup(a => a.IsPaymentMethodAllowed("Payments.Card")).Returns(true);

        var json = await SaveShippingMethodWithOnlyTheCardToPayWith();

        StringAssert.Contains(json, "\"goto_section\":\"confirm_order\"");
        _customerService.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), SystemCustomerFieldNames.SelectedPaymentMethod,
            "Payments.Card", "s1"), Times.Once);
    }
}
