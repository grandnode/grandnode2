using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class CustomerProductViewModelService(
    ICustomerProductService customerProductService,
    IProductService productService,
    IStoreService storeService,
    IVendorService vendorService,
    IRequestTranslationService translationService,
    IEnumTranslationService enumTranslationService)
    : ICustomerProductViewModelService
{
    public virtual async Task<(IEnumerable<CustomerModel.ProductPriceModel> productPriceModels, int totalCount)>
        PrepareProductPriceModel(string customerId, int pageIndex, int pageSize)
    {
        var productPrices =
            await customerProductService.GetProductsPriceByCustomer(customerId, pageIndex - 1, pageSize);
        var items = new List<CustomerModel.ProductPriceModel>();
        foreach (var x in productPrices)
        {
            var m = new CustomerModel.ProductPriceModel {
                Id = x.Id,
                Price = x.Price,
                ProductId = x.ProductId,
                ProductName = (await productService.GetProductById(x.ProductId))?.Name
            };
            items.Add(m);
        }

        return (items, productPrices.TotalCount);
    }

    public virtual async Task<(IEnumerable<CustomerModel.ProductModel> productModels, int totalCount)>
        PreparePersonalizedProducts(string customerId, int pageIndex, int pageSize)
    {
        var products = await customerProductService.GetProductsByCustomer(customerId, pageIndex - 1, pageSize);
        var items = new List<CustomerModel.ProductModel>();
        foreach (var x in products)
        {
            var m = new CustomerModel.ProductModel {
                Id = x.Id,
                DisplayOrder = x.DisplayOrder,
                ProductId = x.ProductId,
                ProductName = (await productService.GetProductById(x.ProductId))?.Name
            };
            items.Add(m);
        }

        return (items, products.TotalCount);
    }

    public virtual async Task<CustomerModel.AddProductModel> PrepareCustomerModelAddProductModel()
    {
        var model = new CustomerModel.AddProductModel();

        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in await storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var v in await vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //product types
        model.AvailableProductTypes = enumTranslationService.ToSelectList(ProductType.SimpleProduct, false).ToList();
        model.AvailableProductTypes.Insert(0, new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = " " });

        return model;
    }

    public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(
        CustomerModel.AddProductModel model, int pageIndex, int pageSize)
    {
        var products = await productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId,
            model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId,
            model.SearchProductName, pageIndex, pageSize);
        return (products.Select(x => x.ToModel()).ToList(), products.TotalCount);
    }

    public virtual async Task InsertCustomerAddProductModel(string customerId, bool personalized,
        CustomerModel.AddProductModel model)
    {
        foreach (var id in model.SelectedProductIds)
        {
            var product = await productService.GetProductById(id);
            if (product != null)
            {
                if (!personalized)
                {
                    if (!(await customerProductService.GetPriceByCustomerProduct(customerId, id)).HasValue)
                        await customerProductService.InsertCustomerProductPrice(new CustomerProductPrice
                            { CustomerId = customerId, ProductId = id, Price = product.Price });
                }
                else
                {
                    if (await customerProductService.GetCustomerProduct(customerId, id) == null)
                        await customerProductService.InsertCustomerProduct(new CustomerProduct
                            { CustomerId = customerId, ProductId = id, DisplayOrder = 0 });
                }
            }
        }
    }

    public virtual async Task UpdateProductPrice(CustomerModel.ProductPriceModel model)
    {
        var productPrice = await customerProductService.GetCustomerProductPriceById(model.Id);
        if (productPrice != null)
        {
            productPrice.Price = model.Price;
            await customerProductService.UpdateCustomerProductPrice(productPrice);
        }
    }

    public virtual async Task DeleteProductPrice(string id)
    {
        var productPrice = await customerProductService.GetCustomerProductPriceById(id);
        if (productPrice == null)
            throw new ArgumentException("No productPrice found with the specified id");

        await customerProductService.DeleteCustomerProductPrice(productPrice);
    }

    public virtual async Task UpdatePersonalizedProduct(CustomerModel.ProductModel model)
    {
        var customerproduct = await customerProductService.GetCustomerProduct(model.Id);
        if (customerproduct != null)
        {
            customerproduct.DisplayOrder = model.DisplayOrder;
            await customerProductService.UpdateCustomerProduct(customerproduct);
        }
    }

    public virtual async Task DeletePersonalizedProduct(string id)
    {
        var customerproduct = await customerProductService.GetCustomerProduct(id);
        if (customerproduct == null)
            throw new ArgumentException("No customerproduct found with the specified id");

        await customerProductService.DeleteCustomerProduct(customerproduct);
    }
}
