using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     E-mails the customer and the store when a recurring payment is cancelled or a cycle fails
/// </summary>
public class RecurringPaymentMessagesHandler : INotificationHandler<RecurringPaymentCancelledEvent>,
    INotificationHandler<RecurringPaymentPastDueEvent>
{
    private readonly ICustomerService _customerService;
    private readonly LanguageSettings _languageSettings;
    private readonly ILogger<RecurringPaymentMessagesHandler> _logger;
    private readonly IRecurringPaymentMessageProviderService _messageProviderService;
    private readonly IOrderService _orderService;

    public RecurringPaymentMessagesHandler(IOrderService orderService, ICustomerService customerService,
        IRecurringPaymentMessageProviderService messageProviderService, LanguageSettings languageSettings,
        ILogger<RecurringPaymentMessagesHandler> logger)
    {
        _orderService = orderService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _languageSettings = languageSettings;
        _logger = logger;
    }

    public Task Handle(RecurringPaymentCancelledEvent notification, CancellationToken cancellationToken)
    {
        return Send(notification.RecurringPayment,
            _messageProviderService.SendRecurringPaymentCancelledCustomerMessage,
            _messageProviderService.SendRecurringPaymentCancelledStoreOwnerMessage);
    }

    public Task Handle(RecurringPaymentPastDueEvent notification, CancellationToken cancellationToken)
    {
        return Send(notification.RecurringPayment,
            _messageProviderService.SendRecurringPaymentFailedCustomerMessage,
            _messageProviderService.SendRecurringPaymentFailedStoreOwnerMessage);
    }

    private async Task Send(RecurringPayment recurringPayment,
        Func<RecurringPayment, Order, Customer?, string?, Task<int>> sendToCustomer,
        Func<RecurringPayment, Order, Customer?, string?, Task<int>> sendToStoreOwner)
    {
        try
        {
            // a deleted order is still loaded, so its number and items stay available
            var initialOrder = await _orderService.GetOrderById(recurringPayment.InitialOrderId);
            if (initialOrder == null)
            {
                _logger.LogWarning(
                    "The initial order {OrderId} of recurring payment {RecurringPaymentId} was not found, no message was sent",
                    recurringPayment.InitialOrderId, recurringPayment.Id);
                return;
            }

            var customer = await _customerService.GetCustomerById(recurringPayment.CustomerId);

            // each send has its own try, so a failing customer message still lets the store owner one go
            await TrySend(recurringPayment,
                () => sendToCustomer(recurringPayment, initialOrder, customer, initialOrder.CustomerLanguageId));
            await TrySend(recurringPayment,
                () => sendToStoreOwner(recurringPayment, initialOrder, customer,
                    _languageSettings.DefaultAdminLanguageId));
        }
        catch (Exception ex)
        {
            // the status is already saved: publishing must complete whatever happens here
            _logger.LogError(ex, "The messages about recurring payment {RecurringPaymentId} could not be sent",
                recurringPayment.Id);
        }
    }

    private async Task TrySend(RecurringPayment recurringPayment, Func<Task<int>> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A message about recurring payment {RecurringPaymentId} could not be sent",
                recurringPayment.Id);
        }
    }
}
