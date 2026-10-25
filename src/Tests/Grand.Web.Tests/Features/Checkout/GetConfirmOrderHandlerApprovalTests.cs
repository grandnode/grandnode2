using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Checkout;
using Grand.Web.Features.Models.Checkout;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Checkout;

[TestClass]
public class GetConfirmOrderHandlerApprovalTests
{
    private static readonly DateTime Created = new(2026, 10, 20, 8, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime CreatedForUser = new(2026, 10, 20, 10, 30, 0);

    private Mock<IOrderApprovalService> _approvals;
    private Customer _customer;
    private GetConfirmOrderHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "buyer", OwnerId = "owner" };
        _approvals = new Mock<IOrderApprovalService>();

        var dates = new Mock<IDateTimeService>();
        dates.Setup(d => d.ConvertToUserTime(Created, DateTimeKind.Utc)).Returns(CreatedForUser);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);

        _handler = new GetConfirmOrderHandler(new Mock<ICountryService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IMediator>().Object, new OrderSettings(),
            new ShippingSettings(), _approvals.Object, dates.Object, translation.Object);
    }

    private void State(bool required, OrderApproval active = null)
    {
        _approvals.Setup(a => a.GetCheckoutState(It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<CalculationContext>()))
            .ReturnsAsync(new OrderApprovalCheckoutState(required, 100, required ? 250 : 50, active));
    }

    private Task<Grand.Web.Models.Checkout.CheckoutConfirmModel> Confirm()
    {
        return _handler.Handle(new GetConfirmOrder {
            Customer = _customer,
            Store = new Grand.Domain.Stores.Store { Id = "s1" },
            Language = new Language { Id = "en" },
            Currency = new Currency { Id = "c1" },
            TaxDisplayType = TaxDisplayType.ExcludingTax,
            Cart = new List<ShoppingCartItem>()
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Confirm_Blocked_SetsRequiredAndMessage()
    {
        State(true);

        var model = await Confirm();

        Assert.IsNotNull(model.OrderApproval);
        Assert.IsTrue(model.OrderApproval.Required);
        Assert.IsFalse(model.OrderApproval.Pending);
        Assert.IsFalse(model.OrderApproval.Approved);
        Assert.AreEqual("OrderApproval.Confirm.Required", model.OrderApproval.Message);
        Assert.IsNull(model.OrderApproval.PendingSince);
        Assert.IsNull(model.OrderApproval.ApprovalId);
    }

    [TestMethod]
    public async Task Confirm_NotInCompany_NoApprovalState()
    {
        _customer.OwnerId = null;
        State(false);

        var model = await Confirm();

        Assert.IsNull(model.OrderApproval);
    }

    [TestMethod]
    public async Task Confirm_UnderTheLimit_NoApprovalState()
    {
        State(false);

        Assert.IsNull((await Confirm()).OrderApproval);
    }

    [TestMethod]
    public async Task Confirm_Pending_SetsPendingSince()
    {
        State(true, new OrderApproval { Id = "a1", Status = OrderApprovalStatus.Pending, CreatedOnUtc = Created });

        var model = await Confirm();

        Assert.IsTrue(model.OrderApproval.Required);
        Assert.IsTrue(model.OrderApproval.Pending);
        Assert.IsFalse(model.OrderApproval.Approved);
        Assert.AreEqual("a1", model.OrderApproval.ApprovalId);
        Assert.AreEqual("OrderApproval.Confirm.Pending", model.OrderApproval.Message);
        Assert.AreEqual(CreatedForUser.ToString("g"), model.OrderApproval.PendingSince);
    }

    [TestMethod]
    public async Task Confirm_Approved_LetsTheOrderThrough()
    {
        State(true, new OrderApproval { Id = "a2", Status = OrderApprovalStatus.Approved, CreatedOnUtc = Created });

        var model = await Confirm();

        Assert.IsTrue(model.OrderApproval.Required);
        Assert.IsTrue(model.OrderApproval.Approved);
        Assert.IsFalse(model.OrderApproval.Pending);
        Assert.AreEqual("a2", model.OrderApproval.ApprovalId);
        Assert.IsNull(model.OrderApproval.PendingSince);
    }

    [TestMethod]
    public async Task Confirm_AskesTheStateOfTheRequestsCustomerAndStore()
    {
        State(false);

        await Confirm();

        _approvals.Verify(a => a.GetCheckoutState(It.IsAny<IList<ShoppingCartItem>>(),
            It.Is<CalculationContext>(c => c.Customer == _customer && c.Store.Id == "s1" &&
                                           c.Currency.Id == "c1" && c.TaxDisplayType == TaxDisplayType.ExcludingTax)));
    }
}
