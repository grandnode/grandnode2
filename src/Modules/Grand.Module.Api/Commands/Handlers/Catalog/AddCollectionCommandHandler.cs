using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddCollectionCommandHandler : IRequestHandler<AddCollectionCommand, CollectionDto>
{
    private readonly ICollectionService _collectionService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    public AddCollectionCommandHandler(
        ICollectionService collectionService,
        ISlugService slugService,
        ISeNameService seNameService)
    {
        _collectionService = collectionService;
        _slugService = slugService;
        _seNameService = seNameService;
    }

    public async Task<CollectionDto> Handle(AddCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = request.Model.ToEntity();
        await _collectionService.InsertCollection(collection);
        request.Model.SeName = await _seNameService.ValidateSeName(collection, request.Model.SeName, collection.Name, true);
        collection.SeName = request.Model.SeName;
        await _collectionService.UpdateCollection(collection);
        await _slugService.SaveSlug(collection, request.Model.SeName, "");

        return collection.ToModel();
    }
}