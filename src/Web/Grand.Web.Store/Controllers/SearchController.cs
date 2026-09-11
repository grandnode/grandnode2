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

// Reduced to a thin subclass of BaseSearchController (ARCH-001) for the Category/Collection/Brand
// picker methods. Store is the only host that scopes these pickers by store - overrides
// PickerStoreId to the current store manager's StaffStoreId, exactly replicating the original
// per-method storeId argument. Restates the attribute set that used to arrive transitively via
// BaseStoreController - see Admin's SearchController for why.
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
    : BaseSearchController(categoryService, brandService, collectionService, adminSearchSettings)
{
    protected override string PickerStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
}
