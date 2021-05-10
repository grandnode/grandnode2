using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Common.Extensions;
using Grand.Infrastructure.Caching;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetCollectionNavigationHandler : IRequestHandler<GetCollectionNavigation, CollectionNavigationModel>
    {
        private readonly ICacheBase _cacheBase;
        private readonly ICollectionService _collectionService;
        private readonly CatalogSettings _catalogSettings;

        public GetCollectionNavigationHandler(ICacheBase cacheBase,
            ICollectionService collectionService,
            CatalogSettings catalogSettings)
        {
            _cacheBase = cacheBase;
            _collectionService = collectionService;
            _catalogSettings = catalogSettings;
        }

        public async Task<CollectionNavigationModel> Handle(GetCollectionNavigation request, CancellationToken cancellationToken)
        {
            string cacheKey = string.Format(CacheKeyConst.COLLECTION_NAVIGATION_MODEL_KEY,
                request.CurrentCollectionId, request.Language.Id, string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            var cacheModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var currentCollection = await _collectionService.GetCollectionById(request.CurrentCollectionId);
                var collections = await _collectionService.GetAllCollections(pageSize: _catalogSettings.CollectionsBlockItemsToDisplay, storeId: request.Store.Id);
                var model = new CollectionNavigationModel
                {
                    TotalCollections = collections.TotalCount
                };

                foreach (var collection in collections)
                {
                    var modelMan = new CollectionBriefInfoModel
                    {
                        Id = collection.Id,
                        Name = collection.GetTranslation(x => x.Name, request.Language.Id),
                        Icon = collection.Icon,
                        SeName = collection.GetSeName(request.Language.Id),
                        IsActive = currentCollection != null && currentCollection.Id == collection.Id,
                    };
                    model.Collections.Add(modelMan);
                }
                return model;
            });
            return cacheModel;
        }
    }
}
