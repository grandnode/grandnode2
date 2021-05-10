using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
                var modelMan = collection.ToModel(request.Language);

                //prepare picture model
                modelMan.PictureModel = new PictureModel
                {
                    Id = collection.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(collection.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(collection.PictureId, _mediaSettings.CollectionThumbPictureSize),
                    Title = string.Format(_translationService.GetResource("Media.Collection.ImageLinkTitleFormat"), modelMan.Name),
                    AlternateText = string.Format(_translationService.GetResource("Media.Collection.ImageAlternateTextFormat"), modelMan.Name)
                };
                model.Add(modelMan);
            }
            return model;
        }
    }
}
