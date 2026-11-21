using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetRecurringPaymentsHandlerTests
{
    private List<RecurringPayment> _payments;
    private List<Order> _orders;
    private GetRecurringPaymentsHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _payments = [];
        _orders = [];

        var recurring = new Mock<IRecurringPaymentService>();
        recurring.Setup(x => x.GetCustomerRecurringPayments("buyer", "s1", 0, 10))
            .ReturnsAsync(() => new PagedList<RecurringPayment>(_payments, 0, 10, _payments.Count));
        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrdersByIds(It.IsAny<string[]>()))
            .ReturnsAsync((string[] ids) => _orders.Where(o => ids.Contains(o.Id)).ToList());
        var products = new Mock<IProductService>();
        products.Setup(x => x.GetProductsByIds(It.IsAny<string[]>(), It.IsAny<CatalogAccess>()))
            .ReturnsAsync((string[] ids, CatalogAccess _) =>
                ids.Contains("p1") ? [new Product { Id = "p1", Name = "Studio Membership" }] : []);
        var dates = new Mock<IDateTimeService>();
        dates.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns((DateTime d, DateTimeKind _) => d);
        var enums = new Mock<IEnumTranslationService>();
        enums.Setup(x => x.GetTranslationEnum(It.IsAny<RecurringPaymentStatus>()))
            .Returns((RecurringPaymentStatus s) => s.ToString());
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key switch {
            "RecurringPayment.CyclesPaid.Of" => "{0} of {1}",
            "RecurringPayment.CyclesPaid.UntilCancelled" => "{0}, until cancelled",
            "RecurringPayment.Cycle.Months" => "Every {0} months",
            _ => key
        });

        _handler = new GetRecurringPaymentsHandler(recurring.Object, orders.Object, products.Object, dates.Object,
            enums.Object, translation.Object);
    }

    private static RecurringPayment Payment(RecurringPaymentStatus status, string initialOrderId = "o1")
    {
        return new RecurringPayment {
            Id = "rp-" + status + "-" + initialOrderId, CustomerId = "buyer", StoreId = "s1",
            InitialOrderId = initialOrderId, Status = status, CycleLength = 1,
            CyclePeriodId = RecurringCyclePeriod.Months, TotalCycles = 12, CyclesPaid = 3,
            NextPaymentDateUtc = new DateTime(2026, 12, 1)
        };
    }

    private static Order Placed(string id, int number, bool deleted = false)
    {
        return new Order {
            Id = id, OrderNumber = number, Deleted = deleted,
            OrderItems = { new OrderItem { ProductId = "p1", Quantity = 1 } }
        };
    }

    private async Task<RecurringPaymentListModel> List()
    {
        return await _handler.Handle(new GetRecurringPayments {
            Customer = new Customer { Id = "buyer" },
            Store = new Grand.Domain.Stores.Store { Id = "s1" },
            Language = new Language { Id = "en" }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task GetRecurringPayments_ActiveMonthly_FillsTheRow()
    {
        _payments.Add(Payment(RecurringPaymentStatus.Active));
        _orders.Add(Placed("o1", 1001));

        var row = (await List()).RecurringPayments.Single();

        Assert.AreEqual("RecurringPayment.Cycle.Month", row.Cycle);
        Assert.AreEqual("3 of 12", row.CyclesPaid);
        Assert.AreEqual(new DateTime(2026, 12, 1), row.NextPaymentDate);
        Assert.AreEqual("o1", row.InitialOrderId);
        Assert.AreEqual(1001, row.InitialOrderNumber);
        Assert.IsTrue(row.CanViewInitialOrder);
        CollectionAssert.AreEqual(new[] { "Studio Membership" }, row.Products.ToArray());
        Assert.IsTrue(row.CanCancel);
        Assert.AreEqual((int)RecurringPaymentStatus.Active, row.StatusId);
        Assert.AreEqual("Active", row.Status);
    }

    [TestMethod]
    public async Task GetRecurringPayments_LongerCycle_UsesThePluralText()
    {
        var payment = Payment(RecurringPaymentStatus.Active);
        payment.CycleLength = 3;
        _payments.Add(payment);

        var row = (await List()).RecurringPayments.Single();

        Assert.AreEqual("Every 3 months", row.Cycle);
    }

    [TestMethod]
    public async Task GetRecurringPayments_UntilCancelled_UsesTheUnlimitedText()
    {
        var payment = Payment(RecurringPaymentStatus.Active);
        payment.TotalCycles = 0;
        _payments.Add(payment);

        var row = (await List()).RecurringPayments.Single();

        Assert.AreEqual("3, until cancelled", row.CyclesPaid);
    }

    [TestMethod]
    public async Task GetRecurringPayments_CancelledOrCompleted_CannotCancelAndHasNoNextPayment()
    {
        _payments.Add(Payment(RecurringPaymentStatus.Cancelled));
        _payments.Add(Payment(RecurringPaymentStatus.Completed));
        _orders.Add(Placed("o1", 1001));

        foreach (var row in (await List()).RecurringPayments)
        {
            Assert.IsFalse(row.CanCancel);
            Assert.IsNull(row.NextPaymentDate);
        }
    }

    [TestMethod]
    public async Task GetRecurringPayments_PendingAndPastDue_CanCancel()
    {
        _payments.Add(Payment(RecurringPaymentStatus.Pending));
        _payments.Add(Payment(RecurringPaymentStatus.PastDue));

        Assert.IsTrue((await List()).RecurringPayments.All(x => x.CanCancel));
    }

    [TestMethod]
    public async Task GetRecurringPayments_InitialOrderMissingOrDeleted_RowWithoutOrderLink()
    {
        _payments.Add(Payment(RecurringPaymentStatus.Active, "missing"));
        _payments.Add(Payment(RecurringPaymentStatus.Active, "deleted"));
        _orders.Add(Placed("deleted", 1002, deleted: true));

        var rows = (await List()).RecurringPayments;

        Assert.AreEqual(2, rows.Count);
        Assert.IsTrue(rows.All(x => !x.CanViewInitialOrder && x.Products.Count == 0));
        Assert.AreEqual(0, rows.Single(x => x.Id == "rp-Active-missing").InitialOrderNumber);
    }

    [TestMethod]
    public async Task GetRecurringPayments_PastDue_FlagsTheRowWithoutTheProviderError()
    {
        var payment = Payment(RecurringPaymentStatus.PastDue);
        payment.LastError = "card_declined: raw";
        _payments.Add(payment);
        _orders.Add(Placed("o1", 1001));

        var row = (await List()).RecurringPayments.Single();

        Assert.IsTrue(row.IsPastDue);
        var texts = row.GetType().GetProperties().Where(p => p.PropertyType == typeof(string))
            .Select(p => (string)p.GetValue(row)).Concat(row.Products);
        Assert.IsFalse(texts.Any(x => x != null && x.Contains("card_declined")));
    }
}
