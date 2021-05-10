using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Domain.Seo;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddBrandCommandHandler : IRequestHandler<AddBrandCommand, BrandDto>
    {
        private readonly IBrandService _brandService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly SeoSettings _seoSettings;

        public AddBrandCommandHandler(
            IBrandService brandService,
            ISlugService slugService,
            ILanguageService languageService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            SeoSettings seoSettings)
        {
            _brandService = brandService;
            _slugService = slugService;
            _languageService = languageService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _seoSettings = seoSettings;
        }

        public async Task<BrandDto> Handle(AddBrandCommand request, CancellationToken cancellationToken)
        {
            var brand = request.Model.ToEntity();
            brand.CreatedOnUtc = DateTime.UtcNow;
            brand.UpdatedOnUtc = DateTime.UtcNow;
            await _brandService.InsertBrand(brand);
            request.Model.SeName = await brand.ValidateSeName(request.Model.SeName, brand.Name, true, _seoSettings, _slugService, _languageService);
            brand.SeName = request.Model.SeName;
            await _brandService.UpdateBrand(brand);
            await _slugService.SaveSlug(brand, request.Model.SeName, "");

            //activity log
            await _customerActivityService.InsertActivity("AddNewBrand", brand.Id, _translationService.GetResource("ActivityLog.AddNewBrand"), brand.Name);

            return brand.ToModel();
        }
    }
}
