using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Admin.Models.ShoppingCart;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface ICustomerViewModelService
    {
        Task<CustomerListModel> PrepareCustomerListModel();
        Task<(IEnumerable<CustomerModel> customerModelList, int totalCount)> PrepareCustomerList(CustomerListModel model,
            string[] searchCustomerGroupIds, string[] searchCustomerTagIds, int pageIndex, int pageSize);
        Task PrepareCustomerModel(CustomerModel model, Customer customer, bool excludeProperties);
        Task<string> ValidateCustomerGroups(IList<CustomerGroup> customerGroups);
        Task<Customer> InsertCustomerModel(CustomerModel model);
        Task<Customer> UpdateCustomerModel(Customer customer, CustomerModel model);
        Task DeleteCustomer(Customer customer);
        Task DeleteSelected(IList<string> selectedIds);
        Task SendEmail(Customer customer, CustomerModel.SendEmailModel model);
        Task<IEnumerable<CustomerModel.LoyaltyPointsHistoryModel>> PrepareLoyaltyPointsHistoryModel(string customerId);
        Task<LoyaltyPointsHistory> InsertLoyaltyPointsHistory(Customer customer, string storeId, int addLoyaltyPointsValue, string addLoyaltyPointsMessage);
        Task<IEnumerable<AddressModel>> PrepareAddressModel(Customer customer);
        Task DeleteAddress(Customer customer, Address address);
        Task PrepareAddressModel(CustomerAddressModel model, Address address, Customer customer, bool excludeProperties);
        Task<Address> InsertAddressModel(Customer customer, CustomerAddressModel model, List<CustomAttribute> customAttributes);
        Task<Address> UpdateAddressModel(Customer customer, Address address, CustomerAddressModel model, List<CustomAttribute> customAttributes);
        Task<IList<ShoppingCartItemModel>> PrepareShoppingCartItemModel(string customerId, int cartTypeId);
        Task DeleteCart(Customer customer, string id);
        Task<IList<string>> UpdateCart(Customer customer, string shoppingCartId, double? unitprice);
        Task<(IEnumerable<CustomerModel.ProductPriceModel> productPriceModels, int totalCount)> PrepareProductPriceModel(string customerId, int pageIndex, int pageSize);
        Task<(IEnumerable<CustomerModel.ProductModel> productModels, int totalCount)> PreparePersonalizedProducts(string customerId, int pageIndex, int pageSize);
        Task<CustomerModel.AddProductModel> PrepareCustomerModelAddProductModel();
        Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerModel.AddProductModel model, int pageIndex, int pageSize);
        Task InsertCustomerAddProductModel(string customerId, bool personalized, CustomerModel.AddProductModel model);
        Task UpdateProductPrice(CustomerModel.ProductPriceModel model);
        Task DeleteProductPrice(string id);
        Task UpdatePersonalizedProduct(CustomerModel.ProductModel model);
        Task DeletePersonalizedProduct(string id);
        Task<(IEnumerable<CustomerModel.ActivityLogModel> activityLogModels, int totalCount)> PrepareActivityLogModel(string customerId, int pageIndex, int pageSize);
        Task<(IEnumerable<ContactFormModel> contactFormModels, int totalCount)> PrepareContactFormModel(string customerId, string vendorId, int pageIndex, int pageSize);
        Task<(IEnumerable<CustomerModel.OutOfStockSubscriptionModel> outOfStockSubscriptionModels, int totalCount)> PrepareOutOfStockSubscriptionModel(string customerId, int pageIndex, int pageSize);
        Task<IList<CustomerModel.CustomerNote>> PrepareCustomerNoteList(string customerId);
        Task<CustomerNote> InsertCustomerNote(string customerId, string downloadId, bool displayToCustomer, string title, string message);
        Task DeleteCustomerNote(string id, string customerId);
    }
}
