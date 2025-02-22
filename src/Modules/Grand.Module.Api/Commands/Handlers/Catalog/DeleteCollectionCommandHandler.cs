﻿using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand, bool>
{
    private readonly ICollectionService _collectionService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public DeleteCollectionCommandHandler(
        ICollectionService collectionService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _collectionService = collectionService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    public async Task<bool> Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionService.GetCollectionById(request.Model.Id);
        if (collection != null) await _collectionService.DeleteCollection(collection);
        return true;
    }
}