using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Services;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class RecurringPaymentMessageProviderServiceTests
{
    private readonly Customer _customer = new() {
        Id = "c1",
        Email = "customer@example.com",
        UserFields = {
            new UserField { Key = SystemCustomerFieldNames.FirstName, Value = "Anna", StoreId = "" },
            new UserField { Key = SystemCustomerFieldNames.LastName, Value = "Nowak", StoreId = "" }
        }
    };
    private readonly Order _initialOrder = new() { Id = "o1", StoreId = "store1", OrderNumber = 1042 };
    private readonly RecurringPayment _recurringPayment =
        new() { Id = "rp1", StoreId = "store1", InitialOrderId = "o1" };
    private Mock<IMessageTemplateService> _messageTemplateServiceMock;
    private List<QueuedEmail> _queued;
    private RecurringPaymentMessageProviderService _service;

    [TestInitialize]
    public void Init()
    {
        _queued = [];
        _messageTemplateServiceMock = new Mock<IMessageTemplateService>();
        var queuedEmailService = new Mock<IQueuedEmailService>();
        queuedEmailService.Setup(x => x.InsertQueuedEmail(It.IsAny<QueuedEmail>())).Callback<QueuedEmail>(_queued.Add);

        var languageService = new Mock<ILanguageService>();
        languageService.Setup(x => x.GetLanguageById(It.IsAny<string>()))
            .ReturnsAsync(new Language { Id = "en", Published = true });
        var emailAccountService = new Mock<IEmailAccountService>();
        emailAccountService.Setup(x => x.GetEmailAccountById(It.IsAny<string>()))
            .ReturnsAsync(new EmailAccount { Id = "ea", Email = "shop@example.com", DisplayName = "Shop" });
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetStoreById("store1"))
            .ReturnsAsync(new Store { Id = "store1", Url = "https://store.example/" });

        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.Is<GetRecurringPaymentTokensCommand>(c => c.RecurringPayment == _recurringPayment),
                default))
            .ReturnsAsync(new LiquidRecurringPayment { CyclePeriod = "Months" });

        var lookup = new MessageLookupService(_messageTemplateServiceMock.Object, languageService.Object,
            emailAccountService.Object, storeService.Object, new EmailAccountSettings());
        _service = new RecurringPaymentMessageProviderService(lookup,
            new MessageProviderService(queuedEmailService.Object), mediator.Object);
    }

    private void TemplateExists(string name)
    {
        _messageTemplateServiceMock.Setup(x => x.GetMessageTemplateByName(name, "store1"))
            .ReturnsAsync(new MessageTemplate {
                Name = name, IsActive = true, Subject = "Every {{RecurringPayment.CyclePeriod}}", Body = "Body"
            });
    }

    private Task<int> Send(string method)
    {
        return method switch {
            nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentCancelledCustomerMessage) =>
                _service.SendRecurringPaymentCancelledCustomerMessage(_recurringPayment, _initialOrder, _customer, "en"),
            nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentCancelledStoreOwnerMessage) =>
                _service.SendRecurringPaymentCancelledStoreOwnerMessage(_recurringPayment, _initialOrder, _customer,
                    "en"),
            nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentFailedCustomerMessage) =>
                _service.SendRecurringPaymentFailedCustomerMessage(_recurringPayment, _initialOrder, _customer, "en"),
            nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentFailedStoreOwnerMessage) =>
                _service.SendRecurringPaymentFailedStoreOwnerMessage(_recurringPayment, _initialOrder, _customer, "en"),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };
    }

    [TestMethod]
    [DataRow(nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentCancelledCustomerMessage),
        "RecurringPayment.Cancelled.CustomerNotification")]
    [DataRow(nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentCancelledStoreOwnerMessage),
        "RecurringPayment.Cancelled.StoreOwnerNotification")]
    [DataRow(nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentFailedCustomerMessage),
        "RecurringPayment.PaymentFailed.CustomerNotification")]
    [DataRow(nameof(RecurringPaymentMessageProviderService.SendRecurringPaymentFailedStoreOwnerMessage),
        "RecurringPayment.PaymentFailed.StoreOwnerNotification")]
    public async Task EachMessage_UsesItsOwnTemplate(string method, string templateName)
    {
        //only this one name has a template, so a method that looks up another name queues nothing
        TemplateExists(templateName);

        var result = await Send(method);

        Assert.IsTrue(result > 0);
        Assert.AreEqual(1, _queued.Count);
    }

    [TestMethod]
    public async Task CancelledCustomerMessage_QueuesToTheCustomer()
    {
        TemplateExists(MessageTemplateNames.RecurringPaymentCancelledCustomerNotification);

        var result = await _service.SendRecurringPaymentCancelledCustomerMessage(_recurringPayment, _initialOrder,
            _customer, "en");

        Assert.IsTrue(result > 0);
        var email = _queued.Single();
        Assert.AreEqual("customer@example.com", email.To);
        Assert.AreEqual("Anna Nowak", email.ToName);
        Assert.AreEqual(_customer.GetFullName(), email.ToName);
        Assert.AreEqual("Every Months", email.Subject);
        Assert.AreEqual(Reference.Order, email.Reference);
        Assert.AreEqual("o1", email.ObjectId);
    }

    [TestMethod]
    public async Task FailedStoreOwnerMessage_QueuesToTheEmailAccount()
    {
        TemplateExists(MessageTemplateNames.RecurringPaymentFailedStoreOwnerNotification);

        var result = await _service.SendRecurringPaymentFailedStoreOwnerMessage(_recurringPayment, _initialOrder,
            _customer, "en");

        Assert.IsTrue(result > 0);
        var email = _queued.Single();
        Assert.AreEqual("shop@example.com", email.To);
        Assert.AreEqual("Shop", email.ToName);
        Assert.AreEqual("Every Months", email.Subject);
        Assert.AreEqual(Reference.Order, email.Reference);
        Assert.AreEqual("o1", email.ObjectId);
    }

    [TestMethod]
    public async Task Message_TemplateMissing_ReturnsZero()
    {
        var result = await _service.SendRecurringPaymentCancelledStoreOwnerMessage(_recurringPayment, _initialOrder,
            _customer, "en");

        Assert.AreEqual(0, result);
        Assert.AreEqual(0, _queued.Count);
    }

    [TestMethod]
    public async Task CustomerMessage_CustomerWithoutEmail_ReturnsZero()
    {
        TemplateExists(MessageTemplateNames.RecurringPaymentFailedCustomerNotification);

        var withoutEmail = await _service.SendRecurringPaymentFailedCustomerMessage(_recurringPayment, _initialOrder,
            new Customer { Id = "c2" }, "en");
        var withoutCustomer = await _service.SendRecurringPaymentFailedCustomerMessage(_recurringPayment,
            _initialOrder, null, "en");

        Assert.AreEqual(0, withoutEmail);
        Assert.AreEqual(0, withoutCustomer);
        Assert.AreEqual(0, _queued.Count);
    }
}
