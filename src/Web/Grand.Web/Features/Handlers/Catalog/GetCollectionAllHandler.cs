using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCollectionAllHandler : IRequestHandler<GetCollectionAll, CollectionListModel>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly ICollectionService _collectionService;
    private readonly MediaSettings _mediaSettings;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;

    public GetCollectionAllHandler(ICollectionService collectionService,
        IPictureService pictureService,
        ITranslationService translationService,
        CatalogSettings catalogSettings,
        MediaSettings mediaSettings)
    {
        _collectionService = collectionService;
        _pictureService = pictureService;
        _translationService = translationService;
        _catalogSettings = catalogSettings;
        _mediaSettings = mediaSettings;
    }

    public async Task<CollectionListModel> Handle(GetCollectionAll request, CancellationToken cancellationToken)
    {
        var model = new CollectionListModel();
        model.CollectionModel = await PrepareCollectionAll(request, model);
        return model;
    }

    private async Task<List<CollectionModel>> PrepareCollectionAll(GetCollectionAll request,
        CollectionListModel collectionListModel)
    {
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;
        if (request.Command.PageSize == 0 || request.Command.PageSize > _catalogSettings.MaxCatalogPageSize)
            request.Command.PageSize = _catalogSettings.MaxCatalogPageSize;

        var model = new List<CollectionModel>();
        var collections = await _collectionService.GetAllCollections(storeId: request.Store.Id,
            pageIndex: request.Command.PageNumber - 1,
            pageSize: request.Command.PageSize);

        collectionListModel.PagingModel.LoadPagedList(collections);
        foreach (var collection in collections) model.Add(await BuildCollection(collection, request));
        return model;
    }

    private async Task<CollectionModel> BuildCollection(Collection collection, GetCollectionAll request)
    {
        var model = collection.ToModel(request.Language);

        //prepare picture model
        var picture = !string.IsNullOrEmpty(collection.PictureId)
            ? await _pictureService.GetPictureById(collection.PictureId)
            : null;
        model.PictureModel = new PictureModel {
            Id = collection.PictureId,
            FullSizeImageUrl = await _pictureService.GetPictureUrl(collection.PictureId),
            ImageUrl = await _pictureService.GetPictureUrl(collection.PictureId,
                _mediaSettings.CollectionThumbPictureSize),
            Style = picture?.Style,
            ExtraField = picture?.ExtraField,
            //"title" attribute
            Title =
                picture != null &&
                !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))
                    ? picture.GetTranslation(x => x.TitleAttribute, request.Language.Id)
                    : string.Format(_translationService.GetResource("Media.Collection.ImageLinkTitleFormat"),
                        model.Name),
            //"alt" attribute
            AlternateText =
                picture != null &&
                !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))
                    ? picture.GetTranslation(x => x.AltAttribute, request.Language.Id)
                    : string.Format(_translationService.GetResource("Media.Collection.ImageAlternateTextFormat"),
                        model.Name)
        };
        return model;
    }
}