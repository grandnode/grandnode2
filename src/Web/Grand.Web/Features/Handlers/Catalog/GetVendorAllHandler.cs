using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Media;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Common;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetVendorAllHandler : IRequestHandler<GetVendorAll, IList<VendorModel>>
    {
        private readonly IVendorService _vendorService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;
        private readonly MediaSettings _mediaSettings;
        private readonly VendorSettings _vendorSettings;

        public GetVendorAllHandler(
            IVendorService vendorService,
            IPictureService pictureService,
            ITranslationService translationService,
            ICacheBase cacheBase,
            IMediator mediator,
            MediaSettings mediaSettings,
            VendorSettings vendorSettings)
        {
            _vendorService = vendorService;
            _pictureService = pictureService;
            _translationService = translationService;
            _cacheBase = cacheBase;
            _mediator = mediator;
            _mediaSettings = mediaSettings;
            _vendorSettings = vendorSettings;
        }

        public async Task<IList<VendorModel>> Handle(GetVendorAll request, CancellationToken cancellationToken)
        {
            return await _cacheBase.GetAsync(CacheKeyConst.VENDOR_ALL_MODEL_KEY, () => PrepareVendorAll(request));
        }

        private async Task<List<VendorModel>> PrepareVendorAll(GetVendorAll request)
        {
            var model = new List<VendorModel>();

            var vendors = await _vendorService.GetAllVendors();
            foreach (var vendor in vendors)
            {
                var vendorModel = new VendorModel
                {
                    Id = vendor.Id,
                    Name = vendor.GetTranslation(x => x.Name, request.Language.Id),
                    Description = vendor.GetTranslation(x => x.Description, request.Language.Id),
                    MetaKeywords = vendor.GetTranslation(x => x.MetaKeywords, request.Language.Id),
                    MetaDescription = vendor.GetTranslation(x => x.MetaDescription, request.Language.Id),
                    MetaTitle = vendor.GetTranslation(x => x.MetaTitle, request.Language.Id),
                    SeName = vendor.GetSeName(request.Language.Id),
                    AllowCustomersToContactVendors = _vendorSettings.AllowCustomersToContactVendors,
                    UserFields = vendor.UserFields
                };

                //prepare vendor address
                vendorModel.Address = await _mediator.Send(new GetVendorAddress()
                {
                    Language = request.Language,
                    Address = vendor.Address,
                    ExcludeProperties = false,
                });

                //prepare picture model
                var pictureModel = new PictureModel
                {
                    Id = vendor.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(vendor.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(vendor.PictureId, _mediaSettings.VendorThumbPictureSize),
                    Title = string.Format(_translationService.GetResource("Media.Vendor.ImageLinkTitleFormat"), vendorModel.Name),
                    AlternateText = string.Format(_translationService.GetResource("Media.Vendor.ImageAlternateTextFormat"), vendorModel.Name)
                };
                vendorModel.PictureModel = pictureModel;
                model.Add(vendorModel);
            }

            return model;
        }
    }
}
