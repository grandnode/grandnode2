using Grand.Business.Checkout.Services.Payments;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Payments;

[TestClass]
public class PaymentTransactionHistoryServiceTests
{
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private IRepository<PaymentTransactionEntry> _repository;
    private PaymentTransactionHistoryService _service;

    public PaymentTransactionHistoryServiceTests()
    {
        _repository = new MongoDBRepositoryTest<PaymentTransactionEntry>();
        _orderServiceMock = new Mock<IOrderService>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _currencyServiceMock.Setup(x => x.GetCurrencyByCode("EUR"))
            .ReturnsAsync(new Currency { CurrencyCode = "EUR", NumberDecimal = 2 });
        _service = new PaymentTransactionHistoryService(_repository, _orderServiceMock.Object,
            _currencyServiceMock.Object);
    }

    [TestMethod]
    public async Task Record_SavesTheEntryAndTheSameOperationAsAnOrderNote()
    {
        var transaction = new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" };

        var entry = await _service.Record(transaction, new Order { Id = "o1" }, PaymentTransactionEntryType.Payment,
            1250, "staff@shop.com", "TR-881", "First instalment");

        var saved = _repository.Table.Single();
        Assert.AreEqual(entry.Id, saved.Id);
        Assert.AreEqual("t1", saved.PaymentTransactionId);
        Assert.AreEqual(PaymentTransactionEntryType.Payment, saved.EntryType);
        Assert.AreEqual(1250, saved.Amount);
        Assert.AreEqual("staff@shop.com", saved.RecordedBy);
        _orderServiceMock.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n => n.OrderId == "o1" &&
            !n.DisplayToCustomer &&
            n.Note == "Payment of 1,250.00 EUR recorded by staff@shop.com. Reference: TR-881. Note: First instalment")),
            Times.Once);
    }

    [TestMethod]
    public async Task Record_WithoutOrder_WritesNoNote()
    {
        await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" }, null,
            PaymentTransactionEntryType.Authorization, 100, "Payments.Card");

        Assert.AreEqual(1, _repository.Table.Count());
        _orderServiceMock.Verify(x => x.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task Record_WithoutRecordedBy_SaysRecorded()
    {
        await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" }, new Order(),
            PaymentTransactionEntryType.Refund, 5, null);

        _orderServiceMock.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n => n.Note == "Refund of 5.00 EUR recorded")),
            Times.Once);
    }

    [TestMethod]
    [DataRow("KWD", 3, 1.235, "Refund of 1.235 KWD recorded")]
    [DataRow("JPY", 0, 1250, "Refund of 1,250 JPY recorded")]
    public async Task Record_WritesTheAmountInTheCurrencyPrecision(string code, int decimals, double amount,
        string expected)
    {
        _currencyServiceMock.Setup(x => x.GetCurrencyByCode(code))
            .ReturnsAsync(new Currency { CurrencyCode = code, NumberDecimal = decimals });

        await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = code }, new Order(),
            PaymentTransactionEntryType.Refund, amount, null);

        _orderServiceMock.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n => n.Note == expected)), Times.Once);
    }

    [TestMethod]
    public async Task Record_DifferenceOfTwoAmounts_IsKeptWithoutTheFloatingPointRemainder()
    {
        var entry = await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" }, null,
            PaymentTransactionEntryType.Payment, 100.3 - 100, null);

        Assert.AreEqual(0.3, entry.Amount);
    }

    [TestMethod]
    public async Task Record_BlankReference_StoresNullAndTrims()
    {
        var entry = await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" }, null,
            PaymentTransactionEntryType.Payment, 10, " staff@shop.com ", "  ", "");

        Assert.AreEqual("staff@shop.com", entry.RecordedBy);
        Assert.IsNull(entry.Reference);
        Assert.IsNull(entry.Note);
    }

    [TestMethod]
    public async Task GetEntries_ReturnsTheTransactionsOwnOldestFirst()
    {
        var later = await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" }, null,
            PaymentTransactionEntryType.Refund, 10, null);
        var earlier = await _service.Record(new PaymentTransaction { Id = "t1", CurrencyCode = "EUR" }, null,
            PaymentTransactionEntryType.Payment, 100, null);
        await _service.Record(new PaymentTransaction { Id = "t2", CurrencyCode = "EUR" }, null,
            PaymentTransactionEntryType.Payment, 50, null);
        await _repository.UpdateField(later.Id, x => x.CreatedOnUtc, new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));
        await _repository.UpdateField(earlier.Id, x => x.CreatedOnUtc, new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc));

        var entries = await _service.GetEntries("t1");

        CollectionAssert.AreEqual(new[] { earlier.Id, later.Id }, entries.Select(x => x.Id).ToArray());
    }
}
