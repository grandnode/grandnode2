using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Catalog.Interfaces.Categories;
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
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
    {
        private readonly ICategoryService _categoryService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IPictureService _pictureService;
        private readonly SeoSettings _seoSettings;

        public UpdateCategoryCommandHandler(
            ICategoryService categoryService,
            ISlugService slugService,
            ILanguageService languageService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IPictureService pictureService,
            SeoSettings seoSettings)
        {
            _categoryService = categoryService;
            _slugService = slugService;
            _languageService = languageService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _pictureService = pictureService;
            _seoSettings = seoSettings;
        }

        public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetCategoryById(request.Model.Id);
            string prevPictureId = category.PictureId;
            category = request.Model.ToEntity(category);
            category.UpdatedOnUtc = DateTime.UtcNow;
            request.Model.SeName = await category.ValidateSeName(request.Model.SeName, category.Name, true, _seoSettings, _slugService, _languageService);
            category.SeName = request.Model.SeName;
            await _categoryService.UpdateCategory(category);
            //search engine name
            await _slugService.SaveSlug(category, request.Model.SeName, "");
            await _categoryService.UpdateCategory(category);
            //delete an old picture (if deleted or updated)
            if (!String.IsNullOrEmpty(prevPictureId) && prevPictureId != category.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }
            //update picture seo file name
            if (!string.IsNullOrEmpty(category.PictureId))
            {
                var picture = await _pictureService.GetPictureById(category.PictureId);
                if (picture != null)
                    await _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(category.Name));
            }
            //activity log
            await _customerActivityService.InsertActivity("EditCategory", category.Id, _translationService.GetResource("ActivityLog.EditCategory"), category.Name);
            return category.ToModel();
        }
    }
}
