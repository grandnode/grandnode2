using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Localization;
using Grand.Web.Commands.Models.Customers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
            await _customerActivityService.InsertActivity("PublicStore.DeleteAccount", "", _translationService.GetResource("ActivityLog.DeleteAccount"), request.Customer);

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
