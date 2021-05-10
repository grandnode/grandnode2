using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Customers;
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
    public partial class CustomerTagViewModelService : ICustomerTagViewModelService
    {
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IProductService _productService;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly ICustomerTagService _customerTagService;
        private readonly IDateTimeService _dateTimeService;

        public CustomerTagViewModelService(
           ITranslationService translationService,
           ICustomerActivityService customerActivityService,
           IProductService productService,
           IStoreService storeService,
           IVendorService vendorService,
           ICustomerTagService customerTagService,
           IDateTimeService dateTimeService)
        {
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _productService = productService;
            _storeService = storeService;
            _vendorService = vendorService;
            _customerTagService = customerTagService;
            _dateTimeService = dateTimeService;
        }

        public virtual CustomerModel PrepareCustomerModelForList(Customer customer)
        {
            return new CustomerModel
            {
                Id = customer.Id,
                Email = !string.IsNullOrEmpty(customer.Email) ? customer.Email : _translationService.GetResource("Admin.Customers.Guest"),
            };
        }
        public virtual CustomerTagModel PrepareCustomerTagModel()
        {
            var model = new CustomerTagModel();
            return model;
        }
        public virtual async Task<CustomerTag> InsertCustomerTagModel(CustomerTagModel model)
        {
            var customertag = model.ToEntity();
            customertag.Name = customertag.Name.ToLower();
            await _customerTagService.InsertCustomerTag(customertag);

            //activity log
            await _customerActivityService.InsertActivity("AddNewCustomerTag", customertag.Id, _translationService.GetResource("ActivityLog.AddNewCustomerTag"), customertag.Name);
            return customertag;
        }
        public virtual async Task<CustomerTag> UpdateCustomerTagModel(CustomerTag customerTag, CustomerTagModel model)
        {
            customerTag = model.ToEntity(customerTag);
            customerTag.Name = customerTag.Name.ToLower();

            await _customerTagService.UpdateCustomerTag(customerTag);

            //activity log
            await _customerActivityService.InsertActivity("EditCustomerTage", customerTag.Id, _translationService.GetResource("ActivityLog.EditCustomerTag"), customerTag.Name);
            return customerTag;
        }
        public virtual async Task DeleteCustomerTag(CustomerTag customerTag)
        {
            //activity log
            await _customerActivityService.InsertActivity("DeleteCustomerTag", customerTag.Id, _translationService.GetResource("ActivityLog.DeleteCustomerTag"), customerTag.Name);
            await _customerTagService.DeleteCustomerTag(customerTag);
        }
        public virtual async Task<CustomerTagProductModel.AddProductModel> PrepareProductModel(string customerTagId)
        {
            var model = new CustomerTagProductModel.AddProductModel();
            model.CustomerTagId = customerTagId;

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
        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerTagProductModel.AddProductModel model, int pageIndex, int pageSize)
        {
            var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }
        public virtual async Task InsertProductModel(CustomerTagProductModel.AddProductModel model)
        {
            foreach (string id in model.SelectedProductIds)
            {
                var product = await _productService.GetProductById(id);
                if (product != null)
                {
                    var customerTagProduct = await _customerTagService.GetCustomerTagProduct(model.CustomerTagId, id);
                    if (customerTagProduct == null)
                    {
                        customerTagProduct = new CustomerTagProduct();
                        customerTagProduct.CustomerTagId = model.CustomerTagId;
                        customerTagProduct.ProductId = id;
                        customerTagProduct.DisplayOrder = 0;
                        await _customerTagService.InsertCustomerTagProduct(customerTagProduct);
                    }
                }
            }
        }

    }
}
