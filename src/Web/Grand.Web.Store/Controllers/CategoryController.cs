using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class CategoryController(
    ICategoryService categoryService,
    ICategoryViewModelService categoryViewModelService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IProductService productService,
    IAdminDataScope<Category> scope)
    : BaseCategoryController(categoryService, categoryViewModelService, languageService,
        translationService, pictureViewModelService, productService, scope)
{
    protected override void EditWarningCheck(Category category)
    {
        if (!category.LimitedToStores ||
            (category.Stores.Contains(Scope.DefaultStoreId) &&
             category.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Categories.Permissions"));
    }
}
