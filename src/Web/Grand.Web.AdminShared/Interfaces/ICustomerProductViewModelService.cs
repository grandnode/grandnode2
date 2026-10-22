using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerProductViewModelService
{
    Task<(IEnumerable<CustomerModel.ProductPriceModel> productPriceModels, int totalCount)> PrepareProductPriceModel(
        string customerId, int pageIndex, int pageSize);

    Task<(IEnumerable<CustomerModel.ProductModel> productModels, int totalCount)> PreparePersonalizedProducts(
        string customerId, int pageIndex, int pageSize);

    Task<CustomerModel.AddProductModel> PrepareCustomerModelAddProductModel();

    Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerModel.AddProductModel model,
        int pageIndex, int pageSize);

    Task InsertCustomerAddProductModel(string customerId, bool personalized, CustomerModel.AddProductModel model);
    Task UpdateProductPrice(CustomerModel.ProductPriceModel model);
    Task DeleteProductPrice(string id);
    Task UpdatePersonalizedProduct(CustomerModel.ProductModel model);
    Task DeletePersonalizedProduct(string id);
}
