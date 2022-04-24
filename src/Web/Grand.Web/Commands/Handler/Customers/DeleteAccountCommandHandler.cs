using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers
{
    public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, bool>
    {
        private readonly ICustomerService _customerService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly LanguageSettings _languageSettings;

        public DeleteAccountCommandHandler(
            ICustomerService customerService,
            IMessageProviderService messageProviderService,
            IQueuedEmailService queuedEmailService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            LanguageSettings languageSettings)
        {
            _customerService = customerService;
            _messageProviderService = messageProviderService;
            _queuedEmailService = queuedEmailService;
            _newsLetterSubscriptionService = newsLetterSubscriptionService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _languageSettings = languageSettings;
        }

        public async Task<bool> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            //activity log
            _ = _customerActivityService.InsertActivity("PublicStore.DeleteAccount", request.Customer.Id,
                request.Customer, request.IpAddress,
                 _translationService.GetResource("ActivityLog.DeleteAccount"), request.Customer);

            //send notification to customer
            await _messageProviderService.SendCustomerDeleteStoreOwnerMessage(request.Customer, _languageSettings.DefaultAdminLanguageId);

            //delete emails
            await _queuedEmailService.DeleteCustomerEmail(request.Customer.Email);

            //delete newsletter subscription
            var newsletter = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(request.Customer.Email, request.Store.Id);
            if (newsletter != null)
                await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletter);

            //delete account
            await _customerService.DeleteCustomer(request.Customer);

            return true;
        }
    }
}
