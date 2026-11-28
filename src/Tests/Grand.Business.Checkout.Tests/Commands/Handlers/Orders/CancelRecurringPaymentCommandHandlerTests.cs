using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class CancelRecurringPaymentCommandHandlerTests
{
    private Mock<IRecurringPaymentService> _service;
    private Mock<IPaymentService> _paymentService;
    private RecurringPayment _recurringPayment;

    public CancelRecurringPaymentCommandHandlerTests()
    {
        _recurringPayment = new RecurringPayment {
            Id = "rp1", Status = RecurringPaymentStatus.Active, ProviderReference = "sub_1"
        };
        _service = new Mock<IRecurringPaymentService>();
        // the service asks the gateway through the callback it is given
        _service.Setup(x => x.Cancel(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Func<RecurringPayment, Task<CancelRecurringPaymentResult>>>()))
            .Returns((string _, string _, Func<RecurringPayment, Task<CancelRecurringPaymentResult>> stopAtGateway) =>
                stopAtGateway(_recurringPayment));
        _paymentService = new Mock<IPaymentService>();
    }

    private CancelRecurringPaymentCommandHandler Handler(IRecurringPaymentService service)
    {
        return new CancelRecurringPaymentCommandHandler(service, _paymentService.Object,
            new Mock<ILogger<CancelRecurringPaymentCommandHandler>>().Object);
    }

    private CancelRecurringPaymentCommand Command()
    {
        return new CancelRecurringPaymentCommand { RecurringPayment = _recurringPayment, CancelledBy = "staff" };
    }

    [TestMethod]
    public async Task Handle_PassesTheProviderResult()
    {
        var refusal = new CancelRecurringPaymentResult();
        refusal.AddError("no");
        _paymentService.Setup(x => x.CancelRecurringPayment(_recurringPayment)).ReturnsAsync(refusal);

        var result = await Handler(_service.Object).Handle(Command(), CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "no" }, result.Errors.ToList());
        _service.Verify(x => x.Cancel("rp1", "staff",
            It.IsAny<Func<RecurringPayment, Task<CancelRecurringPaymentResult>>>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ProviderThrows_ReturnsTheMessageAsAnError()
    {
        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var result = await Handler(_service.Object).Handle(Command(), CancellationToken.None);

        Assert.IsFalse(result.Success);
        CollectionAssert.AreEqual(new[] { "timeout" }, result.Errors.ToList());
    }

    [TestMethod]
    public async Task Handle_ProviderThrows_TheLockIsReleased()
    {
        var repository = new MongoDBRepositoryTest<RecurringPayment>();
        var service = new RecurringPaymentService(repository, new Mock<IProductService>().Object,
            new Mock<IMediator>().Object, new MongoResourceLockTest());
        var rp = new RecurringPayment { Status = RecurringPaymentStatus.Active, ProviderReference = "sub_1" };
        await service.InsertRecurringPayment(rp);
        var command = new CancelRecurringPaymentCommand { RecurringPayment = rp, CancelledBy = "staff" };

        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ThrowsAsync(new HttpRequestException("timeout"));
        Assert.IsFalse((await Handler(service).Handle(command, CancellationToken.None)).Success);
        var recurringPayment = await service.GetRecurringPaymentById(rp.Id);
        Assert.IsNotNull(recurringPayment);
        Assert.AreEqual(RecurringPaymentStatus.Active, recurringPayment.Status);

        _paymentService.Setup(x => x.CancelRecurringPayment(It.IsAny<RecurringPayment>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());
        Assert.IsTrue((await Handler(service).Handle(command, CancellationToken.None)).Success);
        var recurringPayment2 = await service.GetRecurringPaymentById(rp.Id);
        Assert.IsNotNull(recurringPayment2);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, recurringPayment2.Status);
    }

    [TestMethod]
    public async Task Handle_NoCancelledBy_Throws()
    {
        var command = Command();
        command.CancelledBy = "";

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            Handler(_service.Object).Handle(command, CancellationToken.None));
    }
}
