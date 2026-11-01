using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class OrderViewModelServiceApprovalTests
{
    private OrderApproval _approval;
    private Order _order;
    private Mock<IAdminDataScope<Order>> _scope;
    private OrderViewModelService _service;

    [TestInitialize]
    public void Init()
    {
        _order = new Order {
            Id = "o1", OrderNumber = 1042, CustomerId = "buyer",
            OrderStatusId = (int)OrderStatusSystem.AwaitingApproval
        };
        _approval = new OrderApproval {
            OrderId = "o1", CustomerId = "buyer", Status = OrderApprovalStatus.Pending,
            Decider = OrderApprovalDecider.Store, Reasons = ["over the limit"]
        };
        var approvals = new Mock<IOrderApprovalService>();
        approvals.Setup(x => x.GetByOrderId("o1")).ReturnsAsync(() => _approval);

        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "staff" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        _scope = new Mock<IAdminDataScope<Order>>();
        var enums = new Mock<IEnumTranslationService>();
        enums.Setup(x => x.GetTranslationEnum(OrderApprovalStatus.Pending)).Returns("Pending");
        enums.Setup(x => x.GetTranslationEnum(OrderApprovalDecider.Store)).Returns("The store");
        enums.Setup(x => x.GetTranslationEnum(OrderApprovalDecider.Company)).Returns("The company");

        _service = new OrderViewModelService(new Mock<IOrderService>().Object, new Mock<IDateTimeService>().Object,
            new Mock<IPriceFormatter>().Object, new Mock<IDiscountService>().Object,
            new Mock<IRequestTranslationService>().Object, context.Object, new Mock<ICurrencyService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<ICountryService>().Object, new Mock<IProductService>().Object,
            new Mock<IGiftVoucherService>().Object, new Mock<IDownloadService>().Object,
            new Mock<IStoreService>().Object, new Mock<IVendorService>().Object,
            new Mock<ISalesEmployeeService>().Object, new Mock<IAddressAttributeParser>().Object,
            new Mock<IAffiliateService>().Object, new Mock<IPictureService>().Object,
            new Mock<IMerchandiseReturnService>().Object, new Mock<ICustomerService>().Object,
            new Mock<IWarehouseService>().Object, new CurrencySettings(), new TaxSettings(), new AddressSettings(),
            new Mock<IOrderTagService>().Object, new Mock<IOrderStatusService>().Object, new Mock<IMediator>().Object,
            enums.Object, _scope.Object, approvals.Object);
    }

    private async Task<OrderModel> Details()
    {
        var model = new OrderModel();
        await _service.PrepareOrderApprovalDetails(model, _order);
        return model;
    }

    [TestMethod]
    public async Task PrepareOrderDetails_PendingStoreDecided_CanDecide()
    {
        var model = await Details();

        Assert.IsTrue(model.CanDecideApproval);
        Assert.AreEqual("Pending", model.ApprovalStatus);
        Assert.AreEqual("The store", model.ApprovalDecider);
        CollectionAssert.AreEqual(new[] { "over the limit" }, model.ApprovalReasons.ToArray());
    }

    [TestMethod]
    public async Task PrepareOrderDetails_CompanyDecided_CannotDecideButShowsReasons()
    {
        _approval.Decider = OrderApprovalDecider.Company;

        var model = await Details();

        Assert.IsFalse(model.CanDecideApproval);
        Assert.AreEqual("The company", model.ApprovalDecider);
        CollectionAssert.AreEqual(new[] { "over the limit" }, model.ApprovalReasons.ToArray());
    }

    [TestMethod]
    public async Task PrepareOrderDetails_AlreadyDecided_CannotDecide()
    {
        _approval.Status = OrderApprovalStatus.Approved;

        Assert.IsFalse((await Details()).CanDecideApproval);
    }

    [TestMethod]
    public async Task PrepareOrderDetails_OrderNoLongerAwaiting_CannotDecide()
    {
        _order.OrderStatusId = (int)OrderStatusSystem.Cancelled;

        Assert.IsFalse((await Details()).CanDecideApproval);
    }

    [TestMethod]
    public async Task PrepareOrderDetails_VendorScope_CannotDecide()
    {
        _scope.Setup(x => x.DefaultVendorId).Returns("vendor");

        var model = await Details();

        Assert.IsFalse(model.CanDecideApproval);
        Assert.IsNull(model.ApprovalStatus);
        Assert.AreEqual(0, model.ApprovalReasons.Count);
    }

    [TestMethod]
    public async Task PrepareOrderDetails_OwnOrder_CannotDecide()
    {
        _approval.CustomerId = "staff";

        Assert.IsFalse((await Details()).CanDecideApproval);
    }

    [TestMethod]
    public async Task PrepareOrderDetails_NoRequest_LeavesFieldsEmpty()
    {
        _approval = null;

        var model = await Details();

        Assert.IsFalse(model.CanDecideApproval);
        Assert.IsNull(model.ApprovalStatus);
        Assert.IsNull(model.ApprovalDecider);
        Assert.AreEqual(0, model.ApprovalReasons.Count);
    }

    [TestMethod]
    public async Task PrepareOrderApprovalDecisionModel_CanDecide_CarriesTheOrderAndReasons()
    {
        var model = await _service.PrepareOrderApprovalDecisionModel(_order);

        Assert.AreEqual("o1", model.OrderId);
        Assert.AreEqual(1042, model.OrderNumber);
        CollectionAssert.AreEqual(new[] { "over the limit" }, model.Reasons.ToArray());
    }

    [TestMethod]
    public async Task PrepareOrderApprovalDecisionModel_CannotDecide_Null()
    {
        _approval.Decider = OrderApprovalDecider.Company;

        Assert.IsNull(await _service.PrepareOrderApprovalDecisionModel(_order));
    }
}
