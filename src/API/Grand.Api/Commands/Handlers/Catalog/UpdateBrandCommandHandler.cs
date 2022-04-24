using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, BrandDto>
    {
        private readonly IBrandService _brandService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IPictureService _pictureService;
        private readonly IWorkContext _workContext;

        private readonly SeoSettings _seoSettings;

        public UpdateBrandCommandHandler(
            IBrandService brandService,
            ISlugService slugService,
            ILanguageService languageService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IPictureService pictureService,
            IWorkContext workContext,
            SeoSettings seoSettings)
        {
            _brandService = brandService;
            _slugService = slugService;
            _languageService = languageService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _pictureService = pictureService;
            _workContext = workContext;
            _seoSettings = seoSettings;
        }

        public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
        {
            var brand = await _brandService.GetBrandById(request.Model.Id);
            var prevPictureId = brand.PictureId;
            brand = request.Model.ToEntity(brand);
            brand.UpdatedOnUtc = DateTime.UtcNow;
            request.Model.SeName = await brand.ValidateSeName(request.Model.SeName, brand.Name, true, _seoSettings, _slugService, _languageService);
            brand.SeName = request.Model.SeName;
            await _brandService.UpdateBrand(brand);
            //search engine name
            await _slugService.SaveSlug(brand, request.Model.SeName, "");
            await _brandService.UpdateBrand(brand);
            //delete an old picture (if deleted or updated)
            if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != brand.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }
            //update picture seo file name
            if (!string.IsNullOrEmpty(brand.PictureId))
            {
                var picture = await _pictureService.GetPictureById(brand.PictureId);
                if (picture != null)
                    await _pictureService.SetSeoFilename(picture, _pictureService.GetPictureSeName(brand.Name));
            }
            //activity log
            _ = _customerActivityService.InsertActivity("EditBrand", brand.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.EditBrand"), brand.Name);

            return brand.ToModel();
        }
    }
}
