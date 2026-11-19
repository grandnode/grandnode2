using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Stores;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class PaymentReminderServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Today = Now.Date;

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IOrderMessageProviderService> _messageServiceMock;
    private IRepository<Order> _orderRepository;
    private Mock<IOrderService> _orderServiceMock;
    private PaymentReminderService _service;
    private Mock<ISettingService> _settingServiceMock;
    private List<Store> _stores;

    [TestInitialize]
    public void Init()
    {
        _orderRepository = new MongoDBRepositoryTest<Order>();
        _stores = [new Store { Id = "store1" }];
        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(x => x.GetAllStores()).ReturnsAsync(() => _stores);
        _settingServiceMock = new Mock<ISettingService>();
        _settingServiceMock.Setup(x => x.LoadSetting<OrderSettings>(It.IsAny<string>()))
            .ReturnsAsync(new OrderSettings());
        _customerServiceMock = new Mock<ICustomerService>();
        _customerServiceMock.Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Customer { Id = id });
        _orderServiceMock = new Mock<IOrderService>();
        _messageServiceMock = new Mock<IOrderMessageProviderService>();
        _messageServiceMock.Setup(x => x.SendPaymentDueReminderCustomerMessage(It.IsAny<Order>(),
            It.IsAny<Customer>(), It.IsAny<string>())).ReturnsAsync(1);
        _messageServiceMock.Setup(x => x.SendPaymentOverdueCustomerMessage(It.IsAny<Order>(),
            It.IsAny<Customer>(), It.IsAny<string>())).ReturnsAsync(1);

        _service = new PaymentReminderService(_orderRepository, storeServiceMock.Object,
            _settingServiceMock.Object, _customerServiceMock.Object, _orderServiceMock.Object,
            _messageServiceMock.Object, NullLogger<PaymentReminderService>.Instance);
    }

    private async Task<Order> AddOrder(int dueInDays, PaymentStatus paymentStatus = PaymentStatus.Pending,
        OrderStatusSystem orderStatus = OrderStatusSystem.Pending, bool deleted = false, string storeId = "store1",
        string customerId = "c1")
    {
        var order = new Order {
            StoreId = storeId,
            CustomerId = customerId,
            CustomerLanguageId = "lang1",
            PaymentDueDateUtc = Today.AddDays(dueInDays),
            PaymentStatusId = paymentStatus,
            OrderStatusId = (int)orderStatus,
            Deleted = deleted
        };
        await _orderRepository.InsertAsync(order);
        return order;
    }

    private void VerifyReminders(Order order, int times)
    {
        _messageServiceMock.Verify(x => x.SendPaymentDueReminderCustomerMessage(
            It.Is<Order>(o => o.Id == order.Id), It.IsAny<Customer>(), "lang1"), Times.Exactly(times));
    }

    private void VerifyOverdueNotices(Order order, int times)
    {
        _messageServiceMock.Verify(x => x.SendPaymentOverdueCustomerMessage(
            It.Is<Order>(o => o.Id == order.Id), It.IsAny<Customer>(), "lang1"), Times.Exactly(times));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public async Task DueWithinWindow_Reminded(int dueInDays)
    {
        var order = await AddOrder(dueInDays);

        await _service.SendPaymentReminders(Now);

        VerifyReminders(order, 1);
        VerifyOverdueNotices(order, 0);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.AreEqual(Now, stored.PaymentReminderSentOnUtc);
        Assert.IsNull(stored.PaymentOverdueNoticeSentOnUtc);
        _orderServiceMock.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == order.Id && !n.DisplayToCustomer && n.Note == "Payment due reminder e-mail sent")));
    }

    [TestMethod]
    public async Task DueAfterWindow_NotReminded()
    {
        var order = await AddOrder(4);

        await _service.SendPaymentReminders(Now);

        VerifyReminders(order, 0);
        Assert.IsNull((await _orderRepository.GetByIdAsync(order.Id)).PaymentReminderSentOnUtc);
    }

    [TestMethod]
    public async Task RunTwice_SentOnce()
    {
        var dueSoon = await AddOrder(2);
        var overdue = await AddOrder(-1);

        await _service.SendPaymentReminders(Now);
        await _service.SendPaymentReminders(Now.AddHours(1));

        VerifyReminders(dueSoon, 1);
        VerifyOverdueNotices(overdue, 1);
    }

    [TestMethod]
    public async Task Overdue_NotReminded_OnlyOverdueNotice()
    {
        var order = await AddOrder(-5);

        await _service.SendPaymentReminders(Now);

        VerifyReminders(order, 0);
        VerifyOverdueNotices(order, 1);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.AreEqual(Now, stored.PaymentOverdueNoticeSentOnUtc);
        Assert.IsNull(stored.PaymentReminderSentOnUtc);
    }

    [TestMethod]
    public async Task RemindedEarlier_LaterOverdue_GetsOverdueNotice()
    {
        var order = await AddOrder(1);
        await _service.SendPaymentReminders(Now);

        await _service.SendPaymentReminders(Now.AddDays(2));

        VerifyReminders(order, 1);
        VerifyOverdueNotices(order, 1);
    }

    [TestMethod]
    public async Task Paid_Cancelled_Deleted_Skipped()
    {
        var paid = await AddOrder(1, PaymentStatus.Paid);
        var refunded = await AddOrder(-1, PaymentStatus.Refunded);
        var cancelled = await AddOrder(-1, orderStatus: OrderStatusSystem.Cancelled);
        var deleted = await AddOrder(1, deleted: true);
        var partiallyPaid = await AddOrder(-1, PaymentStatus.PartiallyPaid);

        await _service.SendPaymentReminders(Now);

        VerifyReminders(paid, 0);
        VerifyOverdueNotices(refunded, 0);
        VerifyOverdueNotices(cancelled, 0);
        VerifyReminders(deleted, 0);
        VerifyOverdueNotices(partiallyPaid, 1);
    }

    [TestMethod]
    public async Task NotOnCredit_Skipped()
    {
        await _orderRepository.InsertAsync(new Order { StoreId = "store1", PaymentStatusId = PaymentStatus.Pending });

        await _service.SendPaymentReminders(Now);

        _messageServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task OtherStoreSetting_UsedPerStore()
    {
        _stores.Add(new Store { Id = "store2" });
        _settingServiceMock.Setup(x => x.LoadSetting<OrderSettings>("store2"))
            .ReturnsAsync(new OrderSettings { PaymentReminderDaysBefore = 7 });
        var store1 = await AddOrder(6);
        var store2 = await AddOrder(6, storeId: "store2");

        await _service.SendPaymentReminders(Now);

        VerifyReminders(store1, 0);
        VerifyReminders(store2, 1);
    }

    [TestMethod]
    public async Task TemplateInactive_StillMarked()
    {
        _messageServiceMock.Setup(x => x.SendPaymentDueReminderCustomerMessage(It.IsAny<Order>(),
            It.IsAny<Customer>(), It.IsAny<string>())).ReturnsAsync(0);
        var order = await AddOrder(1);

        await _service.SendPaymentReminders(Now);

        Assert.AreEqual(Now, (await _orderRepository.GetByIdAsync(order.Id)).PaymentReminderSentOnUtc);
        _orderServiceMock.Verify(x => x.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task OneOrderFails_OthersSent()
    {
        _customerServiceMock.Setup(x => x.GetCustomerById("broken")).ThrowsAsync(new InvalidOperationException());
        var broken = await AddOrder(1, customerId: "broken");
        var good = await AddOrder(1);

        await _service.SendPaymentReminders(Now);

        VerifyReminders(good, 1);
        Assert.AreEqual(Now, (await _orderRepository.GetByIdAsync(broken.Id)).PaymentReminderSentOnUtc);
    }

    [TestMethod]
    public async Task CancellationRequested_Throws()
    {
        var order = await AddOrder(1);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            _service.SendPaymentReminders(Now, new CancellationToken(true)));

        VerifyReminders(order, 0);
    }
}
