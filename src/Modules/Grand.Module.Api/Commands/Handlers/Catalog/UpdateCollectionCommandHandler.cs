using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class UpdateCollectionCommandHandler : IRequestHandler<UpdateCollectionCommand, CollectionDto>
{
    private readonly ICollectionService _collectionService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;
    public UpdateCollectionCommandHandler(
        ICollectionService collectionService,
        ISlugService slugService,
        IPictureService pictureService,
        ISeNameService seNameService)
    {
        _collectionService = collectionService;
        _slugService = slugService;
        _pictureService = pictureService;
        _seNameService = seNameService;
    }

    public async Task<CollectionDto> Handle(UpdateCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionService.GetCollectionById(request.Model.Id);
        var prevPictureId = collection.PictureId;
        collection = request.Model.ToEntity(collection);
        request.Model.SeName = await _seNameService.ValidateSeName(collection, request.Model.SeName, collection.Name, true);
        collection.SeName = request.Model.SeName;
        await _collectionService.UpdateCollection(collection);
        //search engine name
        await _slugService.SaveSlug(collection, request.Model.SeName, "");
        await _collectionService.UpdateCollection(collection);
        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != collection.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        if (!string.IsNullOrEmpty(collection.PictureId))
        {
            var picture = await _pictureService.GetPictureById(collection.PictureId);
            if (picture != null)
                await _pictureService.SetSeoFilename(picture, _pictureService.GetPictureSeName(collection.Name));
        }

        return collection.ToModel();
    }
}