using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CustomerGroupViewModelService : ICustomerGroupViewModelService
{
    private readonly ICustomerGroupProductService _customerGroupProductService;
    private readonly IGroupService _groupService;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;

    #region Constructors

    public CustomerGroupViewModelService(
        IGroupService groupService,
        ICustomerGroupProductService customerGroupProductService,
        ITranslationService translationService,
        IProductService productService,
        IStoreService storeService,
        IVendorService vendorService)
    {
        _groupService = groupService;
        _customerGroupProductService = customerGroupProductService;
        _translationService = translationService;
        _productService = productService;
        _storeService = storeService;
        _vendorService = vendorService;
    }

    #endregion

    public virtual CustomerGroupModel PrepareCustomerGroupModel(CustomerGroup customerGroup)
    {
        var model = customerGroup.ToModel();
        return model;
    }

    public virtual CustomerGroupModel PrepareCustomerGroupModel()
    {
        var model = new CustomerGroupModel {
            //default values
            Active = true
        };
        return model;
    }

    public virtual async Task<CustomerGroup> InsertCustomerGroupModel(CustomerGroupModel model)
    {
        var customerGroup = model.ToEntity();
        await _groupService.InsertCustomerGroup(customerGroup);
        return customerGroup;
    }

    public virtual async Task<CustomerGroup> UpdateCustomerGroupModel(CustomerGroup customerGroup,
        CustomerGroupModel model)
    {
        customerGroup = model.ToEntity(customerGroup);
        await _groupService.UpdateCustomerGroup(customerGroup);
        return customerGroup;
    }

    public virtual async Task DeleteCustomerGroup(CustomerGroup customerGroup)
    {
        await _groupService.DeleteCustomerGroup(customerGroup);
    }

    public virtual async Task<IList<CustomerGroupProductModel>> PrepareCustomerGroupProductModel(string customerGroupId)
    {
        var products = await _customerGroupProductService.GetCustomerGroupProducts(customerGroupId);
        var model = new List<CustomerGroupProductModel>();
        foreach (var item in products)
        {
            var cr = new CustomerGroupProductModel {
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
        var model = new CustomerGroupProductModel.AddProductModel {
            CustomerGroupId = customerGroupId
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
        CustomerGroupProductModel.AddProductModel model, int pageIndex, int pageSize)
    {
        var products = await _productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel()).ToList(), products.TotalCount);
    }

    public virtual async Task InsertProductModel(CustomerGroupProductModel.AddProductModel model)
    {
        foreach (var id in model.SelectedProductIds)
        {
            var product = await _productService.GetProductById(id);
            if (product != null)
            {
                var customerGroupProduct =
                    await _customerGroupProductService.GetCustomerGroupProduct(model.CustomerGroupId, id);
                if (customerGroupProduct == null)
                {
                    customerGroupProduct = new CustomerGroupProduct {
                        CustomerGroupId = model.CustomerGroupId,
                        ProductId = id,
                        DisplayOrder = 0
                    };
                    await _customerGroupProductService.InsertCustomerGroupProduct(customerGroupProduct);
                }
            }
        }
    }
}