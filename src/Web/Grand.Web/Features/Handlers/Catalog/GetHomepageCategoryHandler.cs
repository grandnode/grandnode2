using Grand.Business.Core.Interfaces.Catalog.Categories;
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
    public class GetHomepageCategoryHandler : IRequestHandler<GetHomepageCategory, IList<CategoryModel>>
    {
        private readonly ICategoryService _categoryService;
        private readonly ICacheBase _cacheBase;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly MediaSettings _mediaSettings;

        public GetHomepageCategoryHandler(
            ICategoryService categoryService,
            ICacheBase cacheBase,
            IPictureService pictureService,
            ITranslationService translationService,
            MediaSettings mediaSettings)
        {
            _categoryService = categoryService;
            _cacheBase = cacheBase;
            _pictureService = pictureService;
            _translationService = translationService;
            _mediaSettings = mediaSettings;
        }

        public async Task<IList<CategoryModel>> Handle(GetHomepageCategory request, CancellationToken cancellationToken)
        {
            string categoriesCacheKey = string.Format(CacheKeyConst.CATEGORY_HOMEPAGE_KEY,
                            string.Join(",", request.Customer.GetCustomerGroupIds()),
                            request.Store.Id,
                            request.Language.Id);

            var model = await _cacheBase.GetAsync(categoriesCacheKey, async () =>
            {
                var cat = new List<CategoryModel>();
                foreach (var x in (await _categoryService.GetAllCategoriesDisplayedOnHomePage()))
                {
                    var catModel = x.ToModel(request.Language);
                    //prepare picture model
                    var picture = await _pictureService.GetPictureById(x.PictureId);
                    catModel.PictureModel = new PictureModel
                    {
                        Id = x.PictureId,
                        FullSizeImageUrl = await _pictureService.GetPictureUrl(x.PictureId),
                        ImageUrl = await _pictureService.GetPictureUrl(x.PictureId, _mediaSettings.CategoryThumbPictureSize),
                        Style = picture?.Style,
                        ExtraField = picture?.ExtraField
                    };
                    //"title" attribute
                    catModel.PictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))) ?
                        picture.GetTranslation(x => x.TitleAttribute, request.Language.Id) :
                        string.Format(_translationService.GetResource("Media.Category.ImageLinkTitleFormat"), x.Name);
                    //"alt" attribute
                    catModel.PictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))) ?
                        picture.GetTranslation(x => x.AltAttribute, request.Language.Id) :
                        string.Format(_translationService.GetResource("Media.Category.ImageAlternateTextFormat"), x.Name);

                    cat.Add(catModel);
                }
                return cat;
            });

            return model;
        }
    }
}
