using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Extensions;
using Grand.Infrastructure.Caching;
using Grand.Domain.Catalog;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetPopularProductTagsHandler : IRequestHandler<GetPopularProductTags, PopularProductTagsModel>
    {
        private readonly ICacheBase _cacheBase;
        private readonly IProductTagService _productTagService;
        private readonly CatalogSettings _catalogSettings;

        public GetPopularProductTagsHandler(
            ICacheBase cacheBase,
            IProductTagService productTagService,
            CatalogSettings catalogSettings)
        {
            _cacheBase = cacheBase;
            _productTagService = productTagService;
            _catalogSettings = catalogSettings;
        }

        public async Task<PopularProductTagsModel> Handle(GetPopularProductTags request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.PRODUCTTAG_POPULAR_MODEL_KEY, request.Language.Id, request.Store.Id);
            var cacheModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var model = new PopularProductTagsModel();

                //get all tags
                var allTags = (await _productTagService
                    .GetAllProductTags())
                    .OrderByDescending(x => x.Count)
                    .ToList();

                var tags = allTags
                    .Take(_catalogSettings.NumberOfProductTags)
                    .ToList();
                //sorting
                tags = tags.OrderBy(x => x.Name).ToList();

                model.TotalTags = allTags.Count;

                foreach (var tag in tags)
                    model.Tags.Add(new ProductTagModel
                    {
                        Id = tag.Id,
                        Name = tag.GetTranslation(y => y.Name, request.Language.Id),
                        SeName = tag.SeName,
                        ProductCount = tag.Count
                    });
                return model;
            });
            return cacheModel;
        }
    }
}
