using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CustomerTagViewModelService : ICustomerTagViewModelService
{
    private readonly ICustomerTagService _customerTagService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;

    public CustomerTagViewModelService(
        ITranslationService translationService,
        IProductService productService,
        IStoreService storeService,
        IVendorService vendorService,
        ICustomerTagService customerTagService)
    {
        _translationService = translationService;
        _productService = productService;
        _storeService = storeService;
        _vendorService = vendorService;
        _customerTagService = customerTagService;
    }

    public virtual CustomerModel PrepareCustomerModelForList(Customer customer)
    {
        return new CustomerModel {
            Id = customer.Id,
            Email = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest")
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
        return customertag;
    }

    public virtual async Task<CustomerTag> UpdateCustomerTagModel(CustomerTag customerTag, CustomerTagModel model)
    {
        customerTag = model.ToEntity(customerTag);
        customerTag.Name = customerTag.Name.ToLower();

        await _customerTagService.UpdateCustomerTag(customerTag);
        return customerTag;
    }

    public virtual async Task DeleteCustomerTag(CustomerTag customerTag)
    {
        await _customerTagService.DeleteCustomerTag(customerTag);
    }

    public virtual async Task<CustomerTagProductModel.AddProductModel> PrepareProductModel(string customerTagId)
    {
        var model = new CustomerTagProductModel.AddProductModel {
            CustomerTagId = customerTagId
        };

        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //product types
        model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        CustomerTagProductModel.AddProductModel model, int pageIndex, int pageSize)
    {
        var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel()).ToList(), products.TotalCount);
    }

    public virtual async Task InsertProductModel(CustomerTagProductModel.AddProductModel model)
    {
        foreach (var id in model.SelectedProductIds)
        {
            var product = await _productService.GetProductById(id);
            if (product != null)
            {
                var customerTagProduct = await _customerTagService.GetCustomerTagProduct(model.CustomerTagId, id);
                if (customerTagProduct == null)
                {
                    customerTagProduct = new CustomerTagProduct {
                        CustomerTagId = model.CustomerTagId,
                        ProductId = id,
                        DisplayOrder = 0
                    };
                    await _customerTagService.InsertCustomerTagProduct(customerTagProduct);
                }
            }
        }
    }
}