using FluentValidation;
using Grand.Infrastructure;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class CategoryProductModelValidator : BaseGrandValidator<CategoryModel.CategoryProductModel>
    {
        public CategoryProductModelValidator(
            IEnumerable<IValidatorConsumer<CategoryModel.CategoryProductModel>> validators,
            ITranslationService translationService, ICategoryService categoryService, IWorkContext workContext)
            : base(validators)
        {
            if (!string.IsNullOrEmpty(workContext.CurrentCustomer.StaffStoreId))
            {
                RuleFor(x => x).MustAsync(async (x, y, context) =>
                {
                    var category = await categoryService.GetCategoryById(x.CategoryId);
                    if (category != null)
                        if (!category.AccessToEntityByStore(workContext.CurrentCustomer.StaffStoreId))
                            return false;

                    return true;
                }).WithMessage(translationService.GetResource("Admin.Catalog.Categories.Permisions"));
            }
        }
    }
}