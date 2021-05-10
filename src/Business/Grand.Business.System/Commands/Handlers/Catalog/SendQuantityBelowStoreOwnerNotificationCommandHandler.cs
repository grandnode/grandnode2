using Grand.Business.Catalog.Commands.Models;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Localization;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

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
