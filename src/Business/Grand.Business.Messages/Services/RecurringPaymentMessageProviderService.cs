using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class RecurringPaymentMessageProviderService : IRecurringPaymentMessageProviderService
{
    #region Ctor

    public RecurringPaymentMessageProviderService(
        IMessageLookupService messageLookupService,
        IMessageProviderService messageProviderService,
        IMediator mediator)
    {
        _messageLookupService = messageLookupService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IMessageLookupService _messageLookupService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Sends a recurring payment message to the customer, or to the e-mail account of the template when
    ///     toStoreOwner is set
    /// </summary>
    protected virtual async Task<int> SendRecurringPaymentMessage(string templateName,
        RecurringPayment recurringPayment, Order initialOrder, Customer? customer, string? languageId,
        bool toStoreOwner)
    {
        ArgumentNullException.ThrowIfNull(recurringPayment);
        ArgumentNullException.ThrowIfNull(initialOrder);

        if (!toStoreOwner && string.IsNullOrEmpty(customer?.Email))
            return 0;

        var store = await _messageLookupService.GetStore(recurringPayment.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(templateName, store.Id);
        if (messageTemplate == null)
            return 0;

        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.LinkHost(null);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokensIfNotNull(customer, store, host, language)
            .AddOrderTokens(initialOrder, customer, store, host)
            .AddRecurringPaymentTokens(recurringPayment, language);

        var liquidObject = await builder.BuildAsync();
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = toStoreOwner ? emailAccount.Email : customer?.Email;
        var toName = toStoreOwner ? emailAccount.DisplayName : customer?.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            language.Id, liquidObject,
            toEmail, toName,
            reference: Reference.Order, objectId: initialOrder.Id);
    }

    /// <summary>
    ///     Sends a 'Recurring payment cancelled' message to the customer
    /// </summary>
    public virtual async Task<int> SendRecurringPaymentCancelledCustomerMessage(RecurringPayment recurringPayment,
        Order initialOrder, Customer? customer, string? languageId)
    {
        return await SendRecurringPaymentMessage(MessageTemplateNames.RecurringPaymentCancelledCustomerNotification,
            recurringPayment, initialOrder, customer, languageId, toStoreOwner: false);
    }

    /// <summary>
    ///     Sends a 'Recurring payment cancelled' message to the store owner
    /// </summary>
    public virtual async Task<int> SendRecurringPaymentCancelledStoreOwnerMessage(RecurringPayment recurringPayment,
        Order initialOrder, Customer? customer, string? languageId)
    {
        return await SendRecurringPaymentMessage(MessageTemplateNames.RecurringPaymentCancelledStoreOwnerNotification,
            recurringPayment, initialOrder, customer, languageId, toStoreOwner: true);
    }

    /// <summary>
    ///     Sends a 'Recurring payment failed' message to the customer
    /// </summary>
    public virtual async Task<int> SendRecurringPaymentFailedCustomerMessage(RecurringPayment recurringPayment,
        Order initialOrder, Customer? customer, string? languageId)
    {
        return await SendRecurringPaymentMessage(MessageTemplateNames.RecurringPaymentFailedCustomerNotification,
            recurringPayment, initialOrder, customer, languageId, toStoreOwner: false);
    }

    /// <summary>
    ///     Sends a 'Recurring payment failed' message to the store owner
    /// </summary>
    public virtual async Task<int> SendRecurringPaymentFailedStoreOwnerMessage(RecurringPayment recurringPayment,
        Order initialOrder, Customer? customer, string? languageId)
    {
        return await SendRecurringPaymentMessage(MessageTemplateNames.RecurringPaymentFailedStoreOwnerNotification,
            recurringPayment, initialOrder, customer, languageId, toStoreOwner: true);
    }

    #endregion
}
