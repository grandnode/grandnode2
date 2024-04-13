using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetCategoryAllHandler : IRequestHandler<GetCategoryAll, CategoryListModel>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly ICategoryService _categoryService;
    private readonly MediaSettings _mediaSettings;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;

    public GetCategoryAllHandler(ICategoryService categoryService,
        IPictureService pictureService,
        ITranslationService translationService,
        MediaSettings mediaSettings,
        CatalogSettings catalogSettings)
    {
        _categoryService = categoryService;
        _pictureService = pictureService;
        _translationService = translationService;
        _mediaSettings = mediaSettings;
        _catalogSettings = catalogSettings;
    }

    public async Task<CategoryListModel> Handle(GetCategoryAll request, CancellationToken cancellationToken)
    {
        var model = new CategoryListModel();
        model.CategoriesModel = await PrepareCategories(request, model);
        return model;
    }

    private async Task<List<CategoryModel>> PrepareCategories(GetCategoryAll request,
        CategoryListModel categoryListModel)
    {
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;
        if (request.Command.PageSize == 0 || request.Command.PageSize > _catalogSettings.MaxCatalogPageSize)
            request.Command.PageSize = _catalogSettings.MaxCatalogPageSize;

        var model = new List<CategoryModel>();
        var categories = await _categoryService.GetAllCategories(storeId: request.Store.Id,
            pageIndex: request.Command.PageNumber - 1,
            pageSize: request.Command.PageSize
        );

        categoryListModel.PagingModel.LoadPagedList(categories);

        foreach (var category in categories) model.Add(await BuildCategory(category, request));

        return model;
    }

    private async Task<CategoryModel> BuildCategory(Category category, GetCategoryAll request)
    {
        var model = category.ToModel(request.Language);

        //prepare picture model
        var picture = !string.IsNullOrEmpty(category.PictureId)
            ? await _pictureService.GetPictureById(category.PictureId)
            : null;
        model.PictureModel = new PictureModel {
            Id = category.PictureId,
            FullSizeImageUrl = await _pictureService.GetPictureUrl(category.PictureId),
            ImageUrl = await _pictureService.GetPictureUrl(category.PictureId, _mediaSettings.BrandThumbPictureSize),
            Style = picture?.Style,
            ExtraField = picture?.ExtraField,
            //"title" attribute
            Title =
                picture != null &&
                !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))
                    ? picture.GetTranslation(x => x.TitleAttribute, request.Language.Id)
                    : string.Format(_translationService.GetResource("Media.Brand.ImageLinkTitleFormat"),
                        category.Name),
            //"alt" attribute
            AlternateText =
                picture != null &&
                !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))
                    ? picture.GetTranslation(x => x.AltAttribute, request.Language.Id)
                    : string.Format(_translationService.GetResource("Media.Brand.ImageAlternateTextFormat"),
                        category.Name)
        };

        return model;
    }
}