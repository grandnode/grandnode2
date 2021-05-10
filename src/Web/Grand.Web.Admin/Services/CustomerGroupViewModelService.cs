using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CustomerGroupViewModelService : ICustomerGroupViewModelService
    {
        private readonly IGroupService _groupService;
        private readonly ICustomerGroupProductService _customerGroupProductService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IProductService _productService;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly IDateTimeService _dateTimeService;

        #region Constructors

        public CustomerGroupViewModelService(
            IGroupService groupService,
            ICustomerGroupProductService customerGroupProductService,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            IProductService productService,
            IStoreService storeService,
            IVendorService vendorService,
            IDateTimeService dateTimeService)
        {
            _groupService = groupService;
            _customerGroupProductService = customerGroupProductService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _productService = productService;
            _storeService = storeService;
            _vendorService = vendorService;
            _dateTimeService = dateTimeService;
        }

        #endregion

        public virtual CustomerGroupModel PrepareCustomerGroupModel(CustomerGroup customerGroup)
        {
            var model = customerGroup.ToModel();
            return model;
        }

        public virtual CustomerGroupModel PrepareCustomerGroupModel()
        {
            var model = new CustomerGroupModel();
            //default values
            model.Active = true;
            return model;
        }

        public virtual async Task<CustomerGroup> InsertCustomerGroupModel(CustomerGroupModel model)
        {
            var customerGroup = model.ToEntity();
            await _groupService.InsertCustomerGroup(customerGroup);
            //activity log
            await _customerActivityService.InsertActivity("AddNewCustomerGroup", customerGroup.Id, _translationService.GetResource("ActivityLog.AddNewCustomerGroup"), customerGroup.Name);
            return customerGroup;
        }
        public virtual async Task<CustomerGroup> UpdateCustomerGroupModel(CustomerGroup customerGroup, CustomerGroupModel model)
        {
            customerGroup = model.ToEntity(customerGroup);
            await _groupService.UpdateCustomerGroup(customerGroup);

            //activity log
            await _customerActivityService.InsertActivity("EditCustomerGroup", customerGroup.Id, _translationService.GetResource("ActivityLog.EditCustomerGroup"), customerGroup.Name);
            return customerGroup;
        }
        public virtual async Task DeleteCustomerGroup(CustomerGroup customerGroup)
        {
            await _groupService.DeleteCustomerGroup(customerGroup);

            //activity log
            await _customerActivityService.InsertActivity("DeleteCustomerGroup", customerGroup.Id, _translationService.GetResource("ActivityLog.DeleteCustomerGroup"), customerGroup.Name);
        }
        public virtual async Task<IList<CustomerGroupProductModel>> PrepareCustomerGroupProductModel(string customerGroupId)
        {
            var products = await _customerGroupProductService.GetCustomerGroupProducts(customerGroupId);
            var model = new List<CustomerGroupProductModel>();
            foreach (var item in products)
            {
                var cr = new CustomerGroupProductModel
                {
                    Id = item.Id,
                    Name = (await _productService.GetProductById(item.ProductId))?.Name,
                    ProductId = item.ProductId,
                    DisplayOrder = item.DisplayOrder
                };
                model.Add(cr);
            }
            return model;
        }
        public virtual async Task<CustomerGroupProductModel.AddProductModel> PrepareProductModel(string customerGroupId)
        {
            var model = new CustomerGroupProductModel.AddProductModel();
            model.CustomerGroupId = customerGroupId;

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            return model;
        }

        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerGroupProductModel.AddProductModel model, int pageIndex, int pageSize)
        {
            var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }
        public virtual async Task InsertProductModel(CustomerGroupProductModel.AddProductModel model)
        {
            foreach (string id in model.SelectedProductIds)
            {
                var product = await _productService.GetProductById(id);
                if (product != null)
                {
                    var customerGroupProduct = await _customerGroupProductService.GetCustomerGroupProduct(model.CustomerGroupId, id);
                    if (customerGroupProduct == null)
                    {
                        customerGroupProduct = new CustomerGroupProduct();
                        customerGroupProduct.CustomerGroupId = model.CustomerGroupId;
                        customerGroupProduct.ProductId = id;
                        customerGroupProduct.DisplayOrder = 0;
                        await _customerGroupProductService.InsertCustomerGroupProduct(customerGroupProduct);
                    }
                }
            }
        }
    }
}
