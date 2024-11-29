﻿using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Localization;
using MediatR;

namespace Grand.Module.ScheduledTasks.Commands.Handlers.Catalog;

public class
    SendQuantityBelowStoreOwnerNotificationCommandHandler : IRequestHandler<SendQuantityBelowStoreOwnerCommand, bool>
{
    private readonly LanguageSettings _languageSettings;
    private readonly IMessageProviderService _messageProviderService;

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
            await _messageProviderService.SendQuantityBelowStoreOwnerMessage(request.Product,
                _languageSettings.DefaultAdminLanguageId);
        else
            await _messageProviderService.SendQuantityBelowStoreOwnerMessage(request.Product,
                request.ProductAttributeCombination, _languageSettings.DefaultAdminLanguageId);

        return true;
    }
}