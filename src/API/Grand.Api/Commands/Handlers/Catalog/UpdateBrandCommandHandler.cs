using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Seo;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly SeoSettings _seoSettings;

        public UpdateBrandCommandHandler(
            IBrandService brandService,
            ISlugService slugService,
            ILanguageService languageService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IPictureService pictureService,
            SeoSettings seoSettings)
        {
            _brandService = brandService;
            _slugService = slugService;
            _languageService = languageService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _pictureService = pictureService;
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
                    await _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(brand.Name));
            }
            //activity log
            await _customerActivityService.InsertActivity("EditBrand", brand.Id, _translationService.GetResource("ActivityLog.EditBrand"), brand.Name);

            return brand.ToModel();
        }
    }
}
