﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

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
