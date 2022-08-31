using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Models.Catalog
{
    public class AddBrandCommandHandler : IRequestHandler<AddBrandCommand, BrandDto>
    {
        private readonly IBrandService _brandService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly SeoSettings _seoSettings;

        public AddBrandCommandHandler(
            IBrandService brandService,
            ISlugService slugService,
            ILanguageService languageService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext,
            SeoSettings seoSettings)
        {
            _brandService = brandService;
            _slugService = slugService;
            _languageService = languageService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
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
            _ = _customerActivityService.InsertActivity("AddNewBrand", brand.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.AddNewBrand"), brand.Name);

            return brand.ToModel();
        }
    }
}
