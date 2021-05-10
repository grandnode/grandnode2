using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
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
    public class GetHomepageBrandsHandler : IRequestHandler<GetHomepageBrands, IList<BrandModel>>
    {
        private readonly ICacheBase _cacheBase;
        private readonly IBrandService _brandService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly MediaSettings _mediaSettings;

        public GetHomepageBrandsHandler(
            ICacheBase cacheBase,
            IBrandService brandService,
            IPictureService pictureService,
            ITranslationService translationService,
            MediaSettings mediaSettings)
        {
            _cacheBase = cacheBase;
            _brandService = brandService;
            _pictureService = pictureService;
            _translationService = translationService;
            _mediaSettings = mediaSettings;
        }

        public async Task<IList<BrandModel>> Handle(GetHomepageBrands request, CancellationToken cancellationToken)
        {
            string brandsCacheKey = string.Format(CacheKeyConst.BRAND_HOMEPAGE_KEY, request.Store.Id, request.Language.Id);

            var model = await _cacheBase.GetAsync(brandsCacheKey, async () =>
            {
                var modelBrands = new List<BrandModel>();
                var allBrands = await _brandService.GetAllBrands(storeId: request.Store.Id);
                foreach (var x in allBrands.Where(x => x.ShowOnHomePage))
                {
                    var brandModel = x.ToModel(request.Language);
                    //prepare picture model
                    brandModel.PictureModel = new PictureModel
                    {
                        Id = x.PictureId,
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                        ImageUrl = await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                        Title = string.Format(_translationService.GetResource("Media.Collection.ImageLinkTitleFormat"), brandModel.Name),
                        AlternateText = string.Format(_translationService.GetResource("Media.Collection.ImageAlternateTextFormat"), brandModel.Name)
                    };
                    modelBrands.Add(brandModel);
                }
                return modelBrands;
            });
            return model;

        }
    }
}
