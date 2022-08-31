﻿using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Infrastructure.Caching;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Grand.Business.Core.Extensions;

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
                var picture = !string.IsNullOrEmpty(brand.PictureId) ? await _pictureService.GetPictureById(brand.PictureId) : null;
                modelBrand.PictureModel = new PictureModel
                {
                    Id = brand.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(brand.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(brand.PictureId, _mediaSettings.BrandThumbPictureSize),
                    Style = picture?.Style,
                    ExtraField = picture?.ExtraField
                };
                //"title" attribute
                modelBrand.PictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))) ?
                    picture.GetTranslation(x => x.TitleAttribute, request.Language.Id) :
                    string.Format(_translationService.GetResource("Media.Brand.ImageLinkTitleFormat"), brand.Name);
                //"alt" attribute
                modelBrand.PictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))) ?
                    picture.GetTranslation(x => x.AltAttribute, request.Language.Id) :
                    string.Format(_translationService.GetResource("Media.Brand.ImageAlternateTextFormat"), brand.Name);

                model.Add(modelBrand);
            }
            return model;
        }
    }
}
