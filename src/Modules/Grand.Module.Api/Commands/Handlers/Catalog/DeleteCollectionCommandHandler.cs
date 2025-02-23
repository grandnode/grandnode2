using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Module.Api.Commands.Models.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand, bool>
{
    private readonly ICollectionService _collectionService;

    public DeleteCollectionCommandHandler(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task<bool> Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionService.GetCollectionById(request.Model.Id);
        if (collection != null) await _collectionService.DeleteCollection(collection);
        return true;
    }
}