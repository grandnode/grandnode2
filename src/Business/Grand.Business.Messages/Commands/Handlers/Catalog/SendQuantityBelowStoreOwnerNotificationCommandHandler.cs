using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using Grand.Mediator;

namespace Grand.Module.ScheduledTasks.Commands.Handlers.Catalog;

public class
    SendQuantityBelowStoreOwnerNotificationCommandHandler : IRequestHandler<SendQuantityBelowStoreOwnerCommand, bool>
{
    private readonly LanguageSettings _languageSettings;
    private readonly ICatalogMessageProviderService _catalogMessageProviderService;

    public SendQuantityBelowStoreOwnerNotificationCommandHandler(
        ICatalogMessageProviderService catalogMessageProviderService,
        LanguageSettings languageSettings)
    {
        _catalogMessageProviderService = catalogMessageProviderService;
        _languageSettings = languageSettings;
    }

    public async Task<bool> Handle(SendQuantityBelowStoreOwnerCommand request, CancellationToken cancellationToken)
    {
        if (request.ProductAttributeCombination == null)
            await _catalogMessageProviderService.SendQuantityBelowStoreOwnerMessage(request.Product,
                _languageSettings.DefaultAdminLanguageId);
        else
            await _catalogMessageProviderService.SendQuantityBelowStoreOwnerMessage(request.Product,
                request.ProductAttributeCombination, _languageSettings.DefaultAdminLanguageId);

        return true;
    }
}