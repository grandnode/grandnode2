﻿using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Catalog
{
    public class ProductCategoryValidator : BaseGrandValidator<ProductCategoryDto>
    {
        public ProductCategoryValidator(IEnumerable<IValidatorConsumer<ProductCategoryDto>> validators, ITranslationService translationService, ICategoryService categoryService)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                var category = await categoryService.GetCategoryById(x.CategoryId);
                if (category == null)
                    return false;
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductCategory.Fields.CategoryId.NotExists"));
        }
    }
}
