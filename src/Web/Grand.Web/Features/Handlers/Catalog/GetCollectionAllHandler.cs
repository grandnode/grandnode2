using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure.Caching;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Grand.Business.Core.Extensions;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCollectionAllHandler : IRequestHandler<GetCollectionAll, IList<CollectionModel>>
    {
        private readonly ICollectionService _collectionService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ICacheBase _cacheBase;
        private readonly MediaSettings _mediaSettings;

        public GetCollectionAllHandler(ICollectionService collectionService,
            IPictureService pictureService,
            ITranslationService translationService,
            ICacheBase cacheBase,
            MediaSettings mediaSettings)
        {
            _collectionService = collectionService;
            _pictureService = pictureService;
            _translationService = translationService;
            _cacheBase = cacheBase;
            _mediaSettings = mediaSettings;
        }

        public async Task<IList<CollectionModel>> Handle(GetCollectionAll request, CancellationToken cancellationToken)
        {
            string cacheKey = string.Format(CacheKeyConst.COLLECTION_ALL_MODEL_KEY,
                request.Language.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            return await _cacheBase.GetAsync(cacheKey, () => PrepareCollectionAll(request));
        }

        private async Task<List<CollectionModel>> PrepareCollectionAll(GetCollectionAll request)
        {
            var model = new List<CollectionModel>();
            var collections = await _collectionService.GetAllCollections(storeId: request.Store.Id);
            foreach (var collection in collections)
            {
                var modelcollection = collection.ToModel(request.Language);

                //prepare picture model
                var picture = !string.IsNullOrEmpty(collection.PictureId) ? await _pictureService.GetPictureById(collection.PictureId) : null;
                modelcollection.PictureModel = new PictureModel
                {
                    Id = collection.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(collection.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(collection.PictureId, _mediaSettings.CollectionThumbPictureSize),
                    Style = picture?.Style,
                    ExtraField = picture?.ExtraField
                };
                //"title" attribute
                modelcollection.PictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))) ?
                    picture.GetTranslation(x => x.TitleAttribute, request.Language.Id) :
                    string.Format(_translationService.GetResource("Media.Collection.ImageLinkTitleFormat"), modelcollection.Name);
                //"alt" attribute
                modelcollection.PictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))) ?
                    picture.GetTranslation(x => x.AltAttribute, request.Language.Id) :
                    string.Format(_translationService.GetResource("Media.Collection.ImageAlternateTextFormat"), modelcollection.Name);

                model.Add(modelcollection);
            }
            return model;
        }
    }
}
