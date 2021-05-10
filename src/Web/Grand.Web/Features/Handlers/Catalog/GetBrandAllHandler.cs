using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
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
    public class GetBrandAllHandler : IRequestHandler<GetBrandAll, IList<BrandModel>>
    {
        private readonly IBrandService _brandService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ICacheBase _cacheBase;
        private readonly MediaSettings _mediaSettings;

        public GetBrandAllHandler(IBrandService brandService,
            IPictureService pictureService,
            ITranslationService translationService,
            ICacheBase cacheBase,
            MediaSettings mediaSettings)
        {
            _brandService = brandService;
            _pictureService = pictureService;
            _translationService = translationService;
            _cacheBase = cacheBase;
            _mediaSettings = mediaSettings;
        }

        public async Task<IList<BrandModel>> Handle(GetBrandAll request, CancellationToken cancellationToken)
        {
            string cacheKey = string.Format(CacheKeyConst.BRAND_ALL_MODEL_KEY,
                request.Language.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            return await _cacheBase.GetAsync(cacheKey, () => PrepareBrandAll(request));
        }

        private async Task<List<BrandModel>> PrepareBrandAll(GetBrandAll request)
        {
            var model = new List<BrandModel>();
            var brands = await _brandService.GetAllBrands(storeId: request.Store.Id);
            foreach (var brand in brands)
            {
                var modelBrand = brand.ToModel(request.Language);

                //prepare picture model
                modelBrand.PictureModel = new PictureModel
                {
                    Id = brand.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(brand.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(brand.PictureId, _mediaSettings.BrandThumbPictureSize),
                    Title = string.Format(_translationService.GetResource("Media.Collection.ImageLinkTitleFormat"), brand.Name),
                    AlternateText = string.Format(_translationService.GetResource("Media.Collection.ImageAlternateTextFormat"), brand.Name)
                };
                model.Add(modelBrand);
            }
            return model;
        }
    }
}
