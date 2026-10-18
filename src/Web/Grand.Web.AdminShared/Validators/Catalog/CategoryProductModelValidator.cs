using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class CategoryProductModelValidator : BaseStoreAccessValidator<CategoryModel.CategoryProductModel, Category>
{
    private readonly ICategoryService _categoryService;
    public CategoryProductModelValidator(
        IEnumerable<IValidatorConsumer<CategoryModel.CategoryProductModel>> validators,
        IRequestTranslationService translationService, ICategoryService categoryService, IContextAccessor contextAccessor)
        : base(validators, translationService, contextAccessor)
    {
        _categoryService = categoryService;
    }
    protected override async Task<Category> GetEntity(CategoryModel.CategoryProductModel model)
    {
        return await _categoryService.GetCategoryById(model.CategoryId);
    }

    protected override string GetPermissionsResourceKey => "Admin.Catalog.Categories.Permissions";
}