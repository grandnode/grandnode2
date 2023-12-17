﻿using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog
{
    public class AddCategoryCommandHandler : IRequestHandler<AddCategoryCommand, CategoryDto>
    {
        private readonly ICategoryService _categoryService;
        private readonly ISlugService _slugService;
        private readonly ILanguageService _languageService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly SeoSettings _seoSettings;

        public AddCategoryCommandHandler(
            ICategoryService categoryService,
            ISlugService slugService,
            ILanguageService languageService,
            ITranslationService translationService,
            IWorkContext workContext,
            SeoSettings seoSettings)
        {
            _categoryService = categoryService;
            _slugService = slugService;
            _languageService = languageService;
            _translationService = translationService;
            _workContext = workContext;
            _seoSettings = seoSettings;
        }

        public async Task<CategoryDto> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = request.Model.ToEntity();
            await _categoryService.InsertCategory(category);
            request.Model.SeName = await category.ValidateSeName(request.Model.SeName,
                category.Name, true, _seoSettings, _slugService, _languageService);
            category.SeName = request.Model.SeName;
            await _categoryService.UpdateCategory(category);
            await _slugService.SaveSlug(category, request.Model.SeName, "");

            return category.ToModel();
        }
    }
}
