using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
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
public class BrandController(
    IBrandViewModelService brandViewModelService,
    IBrandService brandService,
    IStoreService storeService,
    ILanguageService languageService,
    ITranslationService translationService,
    IPictureViewModelService pictureViewModelService,
    IAdminDataScope<Brand> scope)
    : BaseBrandController(brandViewModelService, brandService, storeService, languageService,
        translationService, pictureViewModelService, scope)
{
    protected override void EditWarningCheck(Brand brand)
    {
        if (!brand.LimitedToStores ||
            (brand.Stores.Contains(Scope.DefaultStoreId) &&
             brand.Stores.Count > 1))
            Warning(TranslationService.GetResource("Admin.Catalog.Brands.Permissions"));
    }
}
