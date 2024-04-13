using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Media;
using Grand.Domain.Vendors;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog;

public class GetVendorAllHandler : IRequestHandler<GetVendorAll, VendorListModel>
{
    private readonly CatalogSettings _catalogSettings;
    private readonly MediaSettings _mediaSettings;
    private readonly IMediator _mediator;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly VendorSettings _vendorSettings;

    public GetVendorAllHandler(
        IVendorService vendorService,
        IPictureService pictureService,
        ITranslationService translationService,
        CatalogSettings catalogSettings,
        IMediator mediator,
        MediaSettings mediaSettings,
        VendorSettings vendorSettings)
    {
        _vendorService = vendorService;
        _pictureService = pictureService;
        _translationService = translationService;
        _catalogSettings = catalogSettings;
        _mediator = mediator;
        _mediaSettings = mediaSettings;
        _vendorSettings = vendorSettings;
    }

    public async Task<VendorListModel> Handle(GetVendorAll request, CancellationToken cancellationToken)
    {
        var model = new VendorListModel();
        model.VendorsModel = await PrepareVendors(request, model);
        return model;
    }

    private async Task<List<VendorModel>> PrepareVendors(GetVendorAll request, VendorListModel vendorListModel)
    {
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;
        if (request.Command.PageSize == 0 || request.Command.PageSize > _catalogSettings.MaxCatalogPageSize)
            request.Command.PageSize = _catalogSettings.MaxCatalogPageSize;

        var model = new List<VendorModel>();

        var vendors = await _vendorService.GetAllVendors(
            pageIndex: request.Command.PageNumber - 1,
            pageSize: request.Command.PageSize);

        vendorListModel.PagingModel.LoadPagedList(vendors);

        foreach (var vendor in vendors) model.Add(await BuildVendor(vendor, request));

        return model;
    }

    private async Task<VendorModel> BuildVendor(Domain.Vendors.Vendor vendor, GetVendorAll request)
    {
        var model = new VendorModel {
            Id = vendor.Id,
            Name = vendor.GetTranslation(x => x.Name, request.Language.Id),
            Description = vendor.GetTranslation(x => x.Description, request.Language.Id),
            MetaKeywords = vendor.GetTranslation(x => x.MetaKeywords, request.Language.Id),
            MetaDescription = vendor.GetTranslation(x => x.MetaDescription, request.Language.Id),
            MetaTitle = vendor.GetTranslation(x => x.MetaTitle, request.Language.Id),
            SeName = vendor.GetSeName(request.Language.Id),
            AllowCustomersToContactVendors = _vendorSettings.AllowCustomersToContactVendors,
            UserFields = vendor.UserFields,
            //prepare vendor address
            Address = await _mediator.Send(new GetVendorAddress {
                Language = request.Language,
                Address = vendor.Address,
                ExcludeProperties = false
            })
        };

        //prepare picture model
        var pictureModel = new PictureModel {
            Id = vendor.PictureId,
            FullSizeImageUrl = await _pictureService.GetPictureUrl(vendor.PictureId),
            ImageUrl = await _pictureService.GetPictureUrl(vendor.PictureId, _mediaSettings.VendorThumbPictureSize),
            Title = string.Format(_translationService.GetResource("Media.Vendor.ImageLinkTitleFormat"), model.Name),
            AlternateText = string.Format(_translationService.GetResource("Media.Vendor.ImageAlternateTextFormat"),
                model.Name)
        };
        model.PictureModel = pictureModel;

        return model;
    }
}