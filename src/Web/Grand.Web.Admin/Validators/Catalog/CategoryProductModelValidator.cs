﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class CategoryProductModelValidator : BaseGrandValidator<CategoryModel.CategoryProductModel>
{
    public CategoryProductModelValidator(
        IEnumerable<IValidatorConsumer<CategoryModel.CategoryProductModel>> validators,
        ITranslationService translationService, ICategoryService categoryService, IWorkContextAccessor workContextAccessor)
        : base(validators)
    {
        if (!string.IsNullOrEmpty(workContextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var category = await categoryService.GetCategoryById(x.CategoryId);
                if (category != null)
                    if (!category.AccessToEntityByStore(workContextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                        return false;

                return true;
            }).WithMessage(translationService.GetResource("Admin.Catalog.Categories.Permissions"));
    }
}