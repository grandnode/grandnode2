using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
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

public class GetBrandAllHandler : IRequestHandler<GetBrandAll, BrandListModel>
{
    private readonly IBrandService _brandService;
    private readonly CatalogSettings _catalogSettings;
    private readonly MediaSettings _mediaSettings;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;

    public GetBrandAllHandler(IBrandService brandService,
        IPictureService pictureService,
        ITranslationService translationService,
        MediaSettings mediaSettings,
        CatalogSettings catalogSettings)
    {
        _brandService = brandService;
        _pictureService = pictureService;
        _translationService = translationService;
        _mediaSettings = mediaSettings;
        _catalogSettings = catalogSettings;
    }

    public async Task<BrandListModel> Handle(GetBrandAll request, CancellationToken cancellationToken)
    {
        var model = new BrandListModel();
        model.BrandsModel = await PrepareBrands(request, model);
        return model;
    }

    private async Task<List<BrandModel>> PrepareBrands(GetBrandAll request, BrandListModel brandListModel)
    {
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;
        if (request.Command.PageSize == 0 || request.Command.PageSize > _catalogSettings.MaxCatalogPageSize)
            request.Command.PageSize = _catalogSettings.MaxCatalogPageSize;

        var model = new List<BrandModel>();
        var brands = await _brandService.GetAllBrands(storeId: request.Store.Id,
            pageIndex: request.Command.PageNumber - 1,
            pageSize: request.Command.PageSize
        );

        brandListModel.PagingModel.LoadPagedList(brands);

        foreach (var brand in brands) model.Add(await BuildBrand(brand, request));

        return model;
    }

    private async Task<BrandModel> BuildBrand(Brand brand, GetBrandAll request)
    {
        var model = brand.ToModel(request.Language);

        //prepare picture model
        var picture = !string.IsNullOrEmpty(brand.PictureId)
            ? await _pictureService.GetPictureById(brand.PictureId)
            : null;
        model.PictureModel = new PictureModel {
            Id = brand.PictureId,
            FullSizeImageUrl = await _pictureService.GetPictureUrl(brand.PictureId),
            ImageUrl = await _pictureService.GetPictureUrl(brand.PictureId, _mediaSettings.BrandThumbPictureSize),
            Style = picture?.Style,
            ExtraField = picture?.ExtraField,
            //"title" attribute
            Title =
                picture != null &&
                !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, request.Language.Id))
                    ? picture.GetTranslation(x => x.TitleAttribute, request.Language.Id)
                    : string.Format(_translationService.GetResource("Media.Brand.ImageLinkTitleFormat"),
                        brand.Name),
            //"alt" attribute
            AlternateText =
                picture != null &&
                !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, request.Language.Id))
                    ? picture.GetTranslation(x => x.AltAttribute, request.Language.Id)
                    : string.Format(_translationService.GetResource("Media.Brand.ImageAlternateTextFormat"),
                        brand.Name)
        };

        return model;
    }
}