using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class HandlerTests
{
    private Mock<IPaymentService> _paymentServiceMock;

    [TestInitialize]
    public void Init()
    {
        _paymentServiceMock = new Mock<IPaymentService>();
        _paymentServiceMock.Setup(x => x.SupportCapture(It.IsAny<string>())).Returns(Task.FromResult(true));
        _paymentServiceMock.Setup(x => x.SupportRefund(It.IsAny<string>())).Returns(Task.FromResult(true));
        _paymentServiceMock.Setup(x => x.SupportPartiallyRefund(It.IsAny<string>())).Returns(Task.FromResult(true));
        _paymentServiceMock.Setup(x => x.SupportVoid(It.IsAny<string>())).Returns(Task.FromResult(true));
    }

    [TestMethod]
    public async Task CanCancelOrderQueryHandler_HandleTest_OrderStatus_Pending_True()
    {
        //Arrange
        var canCancelOrderQueryHandler = new CanCancelOrderQueryHandler();
        //Act
        var result = await canCancelOrderQueryHandler.Handle(
            new CanCancelOrderQuery { Order = new Order { OrderStatusId = (int)OrderStatusSystem.Pending } },
            CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanCancelOrderQueryHandler_HandleTest_OrderStatus_Processing_False()
    {
        //Arrange
        var canCancelOrderQueryHandler = new CanCancelOrderQueryHandler();
        //Act
        var result = await canCancelOrderQueryHandler.Handle(
            new CanCancelOrderQuery { Order = new Order { OrderStatusId = (int)OrderStatusSystem.Processing } },
            CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }


    [TestMethod]
    public async Task CanCaptureQueryHandler_HandleTest_TransactionStatus_Authorized_True()
    {
        //Arrange
        var canCaptureQueryHandler = new CanCaptureQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canCaptureQueryHandler.Handle(
            new CanCaptureQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Authorized } },
            CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanCaptureQueryHandler_HandleTest_TransactionStatus_Paid_False()
    {
        //Arrange
        var canCaptureQueryHandler = new CanCaptureQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canCaptureQueryHandler.Handle(
            new CanCaptureQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Paid } },
            CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanMarkPaymentTransactionAsAuthorizedQueryHandler_HandleTest_TransactionStatus_Canceled_False()
    {
        //Arrange
        var canMarkPaymentTransactionAsAuthorizedQueryHandler = new CanMarkPaymentTransactionAsAuthorizedQueryHandler();
        //Act
        var result = await canMarkPaymentTransactionAsAuthorizedQueryHandler.Handle(
            new CanMarkPaymentTransactionAsAuthorizedQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Canceled } },
            CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanMarkPaymentTransactionAsAuthorizedQueryHandler_HandleTest_TransactionStatus_Pending_True()
    {
        //Arrange
        var canMarkPaymentTransactionAsAuthorizedQueryHandler = new CanMarkPaymentTransactionAsAuthorizedQueryHandler();
        //Act
        var result = await canMarkPaymentTransactionAsAuthorizedQueryHandler.Handle(
            new CanMarkPaymentTransactionAsAuthorizedQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Pending } },
            CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanMarkPaymentTransactionAsPaidQueryHandler_HandleTest_TransactionStatus_Authorized_True()
    {
        //Arrange
        var canMarkPaymentTransactionAsPaidQueryHandler = new CanMarkPaymentTransactionAsPaidQueryHandler();
        //Act
        var result = await canMarkPaymentTransactionAsPaidQueryHandler.Handle(
            new CanMarkPaymentTransactionAsPaidQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Authorized } },
            CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanMarkPaymentTransactionAsPaidQueryHandler_HandleTest_TransactionStatus_Canceled_False()
    {
        //Arrange
        var canMarkPaymentTransactionAsPaidQueryHandler = new CanMarkPaymentTransactionAsPaidQueryHandler();
        //Act
        var result = await canMarkPaymentTransactionAsPaidQueryHandler.Handle(
            new CanMarkPaymentTransactionAsPaidQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Canceled } },
            CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanPartiallyPaidOfflineQuery_HandleTest_TransactionStatus_Pending_True()
    {
        //Arrange
        var canPartiallyPaidOfflineQueryHandler = new CanPartiallyPaidOfflineQueryHandler();
        //Act
        var result = await canPartiallyPaidOfflineQueryHandler.Handle(
            new CanPartiallyPaidOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Pending, TransactionAmount = 20 },
                AmountToPaid = 10
            }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanPartiallyPaidOfflineQuery_HandleTest_TransactionStatus_Paid_False()
    {
        //Arrange
        var canPartiallyPaidOfflineQueryHandler = new CanPartiallyPaidOfflineQueryHandler();
        //Act
        var result = await canPartiallyPaidOfflineQueryHandler.Handle(
            new CanPartiallyPaidOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Paid, TransactionAmount = 20 },
                AmountToPaid = 10
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanPartiallyRefundOfflineQueryHandler_HandleTest_TransactionStatus_Pending_False()
    {
        //Arrange
        var canPartiallyRefundOfflineQueryHandler = new CanPartiallyRefundOfflineQueryHandler();
        //Act
        var result = await canPartiallyRefundOfflineQueryHandler.Handle(
            new CanPartiallyRefundOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Pending, TransactionAmount = 20 }
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanPartiallyRefundOfflineQueryHandler_HandleTest_TransactionStatus_Paid_True()
    {
        //Arrange
        var canPartiallyRefundOfflineQueryHandler = new CanPartiallyRefundOfflineQueryHandler();
        //Act
        var result = await canPartiallyRefundOfflineQueryHandler.Handle(
            new CanPartiallyRefundOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Pending, TransactionAmount = 20 }
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanPartiallyRefundQueryHandler_HandleTest_Paid_True()
    {
        //Arrange
        var canPartiallyRefundQueryHandler = new CanPartiallyRefundQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canPartiallyRefundQueryHandler.Handle(
            new CanPartiallyRefundQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Paid, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanPartiallyRefundQueryHandler_HandleTest_Pending_False()
    {
        //Arrange
        var canPartiallyRefundQueryHandler = new CanPartiallyRefundQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canPartiallyRefundQueryHandler.Handle(
            new CanPartiallyRefundQuery
                { PaymentTransaction = new PaymentTransaction { TransactionStatus = TransactionStatus.Pending } },
            CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanRefundOfflineQueryHandler_HandleTest_Paid_True()
    {
        //Arrange
        var canRefundOfflineQueryHandler = new CanRefundOfflineQueryHandler();
        //Act
        var result = await canRefundOfflineQueryHandler.Handle(
            new CanRefundOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Paid, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanRefundOfflineQueryHandler_HandleTest_Pending_False()
    {
        //Arrange
        var canRefundOfflineQueryHandler = new CanRefundOfflineQueryHandler();
        //Act
        var result = await canRefundOfflineQueryHandler.Handle(
            new CanRefundOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Pending, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanRefundQueryHandler_HandleTest_Paid_True()
    {
        //Arrange
        var canRefundQueryHandler = new CanRefundQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canRefundQueryHandler.Handle(
            new CanRefundQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Paid, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanRefundQueryHandler_HandleTest_Paid_False()
    {
        //Arrange
        var canRefundQueryHandler = new CanRefundQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canRefundQueryHandler.Handle(
            new CanRefundQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Pending, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanVoidOfflineQueryHandler_HandleTest_Authorized_True()
    {
        //Arrange
        var canVoidOfflineQueryHandler = new CanVoidOfflineQueryHandler();
        //Act
        var result = await canVoidOfflineQueryHandler.Handle(
            new CanVoidOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Authorized, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanVoidOfflineQueryHandler_HandleTest_PartialPaid_False()
    {
        //Arrange
        var canVoidOfflineQueryHandler = new CanVoidOfflineQueryHandler();
        //Act
        var result = await canVoidOfflineQueryHandler.Handle(
            new CanVoidOfflineQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.PartialPaid, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task CanVoidQueryHandler_HandleTest_Authorized_True()
    {
        //Arrange
        var canVoidQueryHandler = new CanVoidQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canVoidQueryHandler.Handle(
            new CanVoidQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Authorized, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CanVoidQueryHandler_HandleTest_Paid_Fals()
    {
        //Arrange
        var canVoidQueryHandler = new CanVoidQueryHandler(_paymentServiceMock.Object);
        //Act
        var result = await canVoidQueryHandler.Handle(
            new CanVoidQuery {
                PaymentTransaction = new PaymentTransaction
                    { TransactionStatus = TransactionStatus.Paid, TransactionAmount = 10 }
            }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }
}