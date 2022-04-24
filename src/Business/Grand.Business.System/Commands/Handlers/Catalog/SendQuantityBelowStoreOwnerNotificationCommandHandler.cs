using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using MediatR;

namespace Grand.Business.System.Commands.Handlers.Catalog
{
    public class SendQuantityBelowStoreOwnerNotificationCommandHandler : IRequestHandler<SendQuantityBelowStoreOwnerCommand, bool>
    {
        private readonly IMessageProviderService _messageProviderService;
        private readonly LanguageSettings _languageSettings;

        public SendQuantityBelowStoreOwnerNotificationCommandHandler(
            IMessageProviderService messageProviderService,
            LanguageSettings languageSettings)
        {
            _messageProviderService = messageProviderService;
            _languageSettings = languageSettings;
        }

        public async Task<bool> Handle(SendQuantityBelowStoreOwnerCommand request, CancellationToken cancellationToken)
        {
            if (request.ProductAttributeCombination == null)
                await _messageProviderService.SendQuantityBelowStoreOwnerMessage(request.Product, _languageSettings.DefaultAdminLanguageId);
            else
                await _messageProviderService.SendQuantityBelowStoreOwnerMessage(request.Product, request.ProductAttributeCombination, _languageSettings.DefaultAdminLanguageId);

            return true;
        }
    }
}
