using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Admin;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

// Reduced to a thin subclass of BaseSearchController (ARCH-001) for the Category/Collection/Brand
// picker methods. Vendor's original code hardcoded storeId: "" for all 3 (no store filter, same as
// Admin) - the base's default PickerStoreId already matches, no override needed. Restates the
// attribute set that used to arrive transitively via BaseVendorController - see Admin's
// SearchController for why.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class SearchController(
    ICategoryService categoryService,
    IBrandService brandService,
    ICollectionService collectionService,
    AdminSearchSettings adminSearchSettings)
    : BaseSearchController(categoryService, brandService, collectionService, adminSearchSettings)
{
}
