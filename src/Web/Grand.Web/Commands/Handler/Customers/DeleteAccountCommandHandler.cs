using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, bool>
{
    private readonly ICustomerService _customerService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMessageProviderService _messageProviderService;
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private readonly IQueuedEmailService _queuedEmailService;

    public DeleteAccountCommandHandler(
        ICustomerService customerService,
        IMessageProviderService messageProviderService,
        IQueuedEmailService queuedEmailService,
        INewsLetterSubscriptionService newsLetterSubscriptionService,
        LanguageSettings languageSettings)
    {
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _queuedEmailService = queuedEmailService;
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _languageSettings = languageSettings;
    }

    public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        //send notification to customer
        await _messageProviderService.SendCustomerDeleteStoreOwnerMessage(request.Customer,
            _languageSettings.DefaultAdminLanguageId);

        //delete emails
        await _queuedEmailService.DeleteCustomerEmail(request.Customer.Email);

        //delete newsletter subscription
        var newsletter =
            await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(request.Customer.Email,
                request.Store.Id);
        if (newsletter != null)
            await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletter);

        //delete account
        await _customerService.DeleteCustomer(request.Customer);

        return true;
    }
}