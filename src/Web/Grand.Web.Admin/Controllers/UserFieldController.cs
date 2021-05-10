using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Business.Marketing.Interfaces.Courses;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Domain.Customers;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.UserFields)]
    public partial class UserFieldController : BaseAdminController
    {
        #region Fields

        private readonly IUserFieldService _userFieldService;
        private readonly ICacheBase _cacheBase;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGroupService _groupService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Constructors

        public UserFieldController(IUserFieldService userFieldService, ICacheBase cacheBase, ITranslationService translationService,
            IWorkContext workContext, IGroupService groupService, IPermissionService permissionService, IServiceProvider serviceProvider)
        {
            _userFieldService = userFieldService;
            _cacheBase = cacheBase;
            _translationService = translationService;
            _workContext = workContext;
            _groupService = groupService;
            _permissionService = permissionService;
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Utilities

        protected async Task<bool> CheckPermission(string objectType, string entityId)
        {
            Enum.TryParse(objectType, out EntityType objType);
            if (objType == EntityType.Category)
            {
                return await PermissionForCategory(entityId);
            }
            if (objType == EntityType.Product)
            {
                return await PermissionForProduct(entityId);
            }
            if (objType == EntityType.Collection)
            {
                return await PermissionForCollection(entityId);
            }
            if (objType == EntityType.Course)
            {
                return await PermissionForCourse(entityId);
            }
            if (objType == EntityType.Order)
            {
                return await PermissionForOrder(entityId);
            }
            if (objType == EntityType.Customer)
            {
                if (!await _permissionService.Authorize(StandardPermission.ManageCustomers))
                    return false;
            }
            if (objType == EntityType.CustomerGroup)
            {
                if (!await _permissionService.Authorize(StandardPermission.ManageCustomerGroups))
                    return false;
            }
            if (objType == EntityType.Vendor)
            {
                if (!await _permissionService.Authorize(StandardPermission.ManageVendors))
                    return false;
            }
            if (objType == EntityType.Shipment)
            {
                if (!await _permissionService.Authorize(StandardPermission.ManageShipments))
                    return false;
            }
            if (objType == EntityType.MerchandiseReturn)
            {
                if (!await _permissionService.Authorize(StandardPermission.ManageMerchandiseReturns))
                    return false;
            }
            if (objType == EntityType.Page)
            {
                if (!await _permissionService.Authorize(StandardPermission.ManagePages))
                    return false;
            }
            if (objType == EntityType.BlogPost)
            {
                return await PermissionForBlog(entityId);
            }
            return true;
        }
        protected async Task<bool> PermissionForCategory(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageCategories))
                return false;

            var categoryService = _serviceProvider.GetRequiredService<ICategoryService>();
            var category = await categoryService.GetCategoryById(id);
            if (category != null)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    if (!category.LimitedToStores || (category.LimitedToStores && category.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && category.Stores.Count > 1))
                        return false;
                    else
                    {
                        if (!category.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                            return false;
                    }
                }
                return true;
            }
            return false;
        }
        protected async Task<bool> PermissionForProduct(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
                return false;

            var productService = _serviceProvider.GetRequiredService<IProductService>();
            var product = await productService.GetProductById(id);
            if (product != null)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    if (!product.LimitedToStores || (product.LimitedToStores && product.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && product.Stores.Count > 1))
                        return false;
                    else
                    {
                        if (!product.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                            return false;
                    }
                }
                if (_workContext.CurrentVendor != null)
                {
                    if (product.VendorId != _workContext.CurrentVendor.Id)
                        return false;
                }
                return true;
            }
            return false;
        }
        protected async Task<bool> PermissionForCollection(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageCollections))
                return false;

            var collectionService = _serviceProvider.GetRequiredService<ICollectionService>();
            var collection = await collectionService.GetCollectionById(id);
            if (collection != null)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    if (!collection.LimitedToStores || (collection.LimitedToStores && collection.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && collection.Stores.Count > 1))
                        return false;
                    else
                    {
                        if (!collection.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                            return false;
                    }
                }
                return true;
            }
            return false;
        }
        protected async Task<bool> PermissionForCourse(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageCourses))
                return false;

            var courseService = _serviceProvider.GetRequiredService<ICourseService>();
            var course = await courseService.GetById(id);
            if (course != null)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    if (!course.LimitedToStores || (course.LimitedToStores && course.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && course.Stores.Count > 1))
                        return false;
                    else
                    {
                        if (!course.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                            return false;
                    }
                }
                return true;
            }
            return false;
        }
        protected async Task<bool> PermissionForOrder(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return false;

            var orderService = _serviceProvider.GetRequiredService<IOrderService>();
            var order = await orderService.GetOrderById(id);
            if (order != null)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    if (order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
                        return false;
                }
                if (_workContext.CurrentVendor != null)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        protected async Task<bool> PermissionForBlog(string id)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageBlog))
                return false;

            var blogService = _serviceProvider.GetRequiredService<IBlogService>();
            var blog = await blogService.GetBlogPostById(id);
            if (blog != null)
            {
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    if (!blog.LimitedToStores || (blog.LimitedToStores && blog.Stores.Contains(_workContext.CurrentCustomer.StaffStoreId) && blog.Stores.Count > 1))
                        return false;
                    else
                    {
                        if (!blog.AccessToEntityByStore(_workContext.CurrentCustomer.StaffStoreId))
                            return false;
                    }
                }
                return true;
            }
            return false;
        }

        #endregion


        #region Methods

        [HttpPost]
        public async Task<IActionResult> Add(UserFieldModel model)
        {
            if (!await CheckPermission(model.ObjectType, model.Id))
            {
                ModelState.AddModelError("", _translationService.GetResource("Admin.Common.UserFields.Permission"));
            }

            if (ModelState.IsValid)
            {
                if (model.SelectedTab > 0)
                    TempData["Grand.selected-tab-index"] = model.SelectedTab;

                await _userFieldService.SaveField(model.ObjectType, model.Id, model.Key, model.Value, model.StoreId);

                //TO DO - temporary solution
                //After add new attribute we need clear cache
                await _cacheBase.Clear();

                return Json(new
                {
                    success = true,
                });
            }
            return Json(new
            {
                success = false,
                errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                               .Select(m => m.ErrorMessage).ToArray()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(UserFieldModel model)
        {
            if (!await CheckPermission(model.ObjectType, model.Id))
            {
                ModelState.AddModelError("", _translationService.GetResource("Admin.Common.UserFields.Permission"));
            }

            if (ModelState.IsValid)
            {
                if (model.SelectedTab > 0)
                    TempData["Grand.selected-tab-index"] = model.SelectedTab;

                await _userFieldService.SaveField(model.ObjectType, model.Id, model.Key, string.Empty, model.StoreId);
                //TO DO - temporary solution
                //After delete attribute we need clear cache
                await _cacheBase.Clear();
                return Json(new
                {
                    success = true,
                });
            }
            return Json(new
            {
                success = false,
                errors = ModelState.Keys.SelectMany(k => ModelState[k].Errors)
                               .Select(m => m.ErrorMessage).ToArray()
            });
        }

        #endregion

    }
}