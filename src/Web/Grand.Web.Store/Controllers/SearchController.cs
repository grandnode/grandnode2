using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Admin;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class SearchController(
    ICategoryService categoryService,
    IBrandService brandService,
    ICollectionService collectionService,
    AdminSearchSettings adminSearchSettings,
    IContextAccessor contextAccessor)
    : BaseSearchController(categoryService, brandService, collectionService, adminSearchSettings, contextAccessor)
{
    protected override string PickerStoreId => CurrentCustomer.StaffStoreId;
}
