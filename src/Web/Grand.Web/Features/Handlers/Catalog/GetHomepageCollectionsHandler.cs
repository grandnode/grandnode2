using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Domain.Media;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetHomepageCollectionsHandler : IRequestHandler<GetHomepageCollections, IList<CollectionModel>>
    {
        private readonly ICacheBase _cacheBase;
        private readonly ICollectionService _collectionService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly MediaSettings _mediaSettings;

        public GetHomepageCollectionsHandler(
            ICacheBase cacheBase,
            ICollectionService collectionService,
            IPictureService pictureService,
            ITranslationService translationService,
            MediaSettings mediaSettings)
        {
            _cacheBase = cacheBase;
            _collectionService = collectionService;
            _pictureService = pictureService;
            _translationService = translationService;
            _mediaSettings = mediaSettings;
        }

        public async Task<IList<CollectionModel>> Handle(GetHomepageCollections request, CancellationToken cancellationToken)
        {
            string collectionsCacheKey = string.Format(CacheKeyConst.COLLECTION_HOMEPAGE_KEY, request.Store.Id, request.Language.Id);

            var model = await _cacheBase.GetAsync(collectionsCacheKey, async () =>
            {
                var modelCollect = new List<CollectionModel>();
                var allcollections = await _collectionService.GetAllCollections(storeId: request.Store.Id);
                foreach (var x in allcollections.Where(x => x.ShowOnHomePage))
                {
                    var _model = x.ToModel(request.Language);
                    //prepare picture model
                    _model.PictureModel = new PictureModel
                    {
                        Id = x.PictureId,
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                        ImageUrl = await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                        Title = string.Format(_translationService.GetResource("Media.Collection.ImageLinkTitleFormat"), _model.Name),
                        AlternateText = string.Format(_translationService.GetResource("Media.Collection.ImageAlternateTextFormat"), _model.Name)
                    };
                    modelCollect.Add(_model);
                }
                return modelCollect;
            });
            return model;

        }
    }
}
