using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System.Linq.Expressions;

namespace Grand.Web.Common.RequestCustomerResolvers;

/// <summary>
///     Saves the request's unsaved guest before the first write to it. Every customer write is a partial update by
///     id, which silently does nothing on a document that does not exist yet
/// </summary>
public class TransientGuestCustomerService(ICustomerService inner, IGuestCustomerPersistence persistence)
    : ICustomerService
{
    private async Task Persist(string customerId)
    {
        if (persistence.IsTransient(customerId)) await persistence.EnsurePersisted();
    }

    #region Reads

    public Task<IPagedList<Customer>> GetAllCustomers(DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        string affiliateId = "", string vendorId = "", string storeId = "", string ownerId = "",
        string salesEmployeeId = "", string[] customerGroupIds = null, string[] customerTagIds = null,
        string email = null, string username = null, string firstName = null, string lastName = null,
        string company = null, string phone = null, string zipPostalCode = null,
        bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null, int pageIndex = 0,
        int pageSize = int.MaxValue, Expression<Func<Customer, object>> orderBySelector = null)
    {
        return inner.GetAllCustomers(createdFromUtc, createdToUtc, affiliateId, vendorId, storeId, ownerId,
            salesEmployeeId, customerGroupIds, customerTagIds, email, username, firstName, lastName, company, phone,
            zipPostalCode, loadOnlyWithShoppingCart, sct, pageIndex, pageSize, orderBySelector);
    }

    public Task<IPagedList<Customer>> GetOnlineCustomers(DateTime lastActivityFromUtc, string[] customerGroupIds,
        string storeId = "", string salesEmployeeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return inner.GetOnlineCustomers(lastActivityFromUtc, customerGroupIds, storeId, salesEmployeeId, pageIndex,
            pageSize);
    }

    public Task<int> GetCountOnlineShoppingCart(DateTime lastActivityFromUtc, string storeId = "",
        string salesEmployeeId = "")
    {
        return inner.GetCountOnlineShoppingCart(lastActivityFromUtc, storeId, salesEmployeeId);
    }

    public Task<Customer> GetCustomerById(string customerId)
    {
        //the request's own unsaved guest, e.g. reloaded by a handler on the first request
        return persistence.IsTransient(customerId)
            ? Task.FromResult(persistence.Transient)
            : inner.GetCustomerById(customerId);
    }

    public Task<IList<Customer>> GetCustomersByIds(string[] customerIds) => inner.GetCustomersByIds(customerIds);

    public Task<Customer> GetCustomerByGuid(Guid customerGuid) => inner.GetCustomerByGuid(customerGuid);

    public Task<Customer> GetCustomerByEmail(string email, string storeId = "") =>
        inner.GetCustomerByEmail(email, storeId);

    public Task<Customer> GetCustomerBySystemName(string systemName) => inner.GetCustomerBySystemName(systemName);

    public Task<Customer> GetCustomerByUsername(string username, string storeId = "") =>
        inner.GetCustomerByUsername(username, storeId);

    #endregion

    #region Inserts

    public Task<Customer> InsertGuestCustomer(Customer customer) => inner.InsertGuestCustomer(customer);

    public Task InsertCustomer(Customer customer) => inner.InsertCustomer(customer);

    public Task<int> DeleteGuestCustomers(DateTime? createdFromUtc, DateTime? createdToUtc,
        bool onlyWithoutShoppingCart) =>
        inner.DeleteGuestCustomers(createdFromUtc, createdToUtc, onlyWithoutShoppingCart);

    #endregion

    #region Writes to one customer

    public async Task UpdateCustomer(Customer customer)
    {
        await Persist(customer?.Id);
        await inner.UpdateCustomer(customer);
    }

    public async Task UpdateUserField<TPropType>(Customer customer, string key, TPropType value, string storeId = "")
    {
        await Persist(customer?.Id);
        await inner.UpdateUserField(customer, key, value, storeId);
    }

    public async Task DeleteCustomer(Customer customer, bool hard = false)
    {
        await Persist(customer?.Id);
        await inner.DeleteCustomer(customer, hard);
    }

    public async Task UpdateCustomerField<T>(Customer customer, Expression<Func<Customer, T>> expression, T value)
    {
        await Persist(customer?.Id);
        await inner.UpdateCustomerField(customer, expression, value);
    }

    public async Task UpdateCustomerField<T>(string customerId, Expression<Func<Customer, T>> expression, T value)
    {
        await Persist(customerId);
        await inner.UpdateCustomerField(customerId, expression, value);
    }

    public async Task UpdateActive(Customer customer)
    {
        await Persist(customer?.Id);
        await inner.UpdateActive(customer);
    }

    public async Task UpdateContributions(Customer customer)
    {
        await Persist(customer?.Id);
        await inner.UpdateContributions(customer);
    }

    public async Task UpdateCustomerLastLoginDate(Customer customer)
    {
        await Persist(customer?.Id);
        await inner.UpdateCustomerLastLoginDate(customer);
    }

    public async Task UpdateCustomerInAdminPanel(Customer customer)
    {
        await Persist(customer?.Id);
        await inner.UpdateCustomerInAdminPanel(customer);
    }

    public async Task ResetCheckoutData(Customer customer, string storeId, bool clearCouponCodes = false,
        bool clearCheckoutAttributes = false, bool clearLoyaltyPoints = true, bool clearShipping = true,
        bool clearPayment = true)
    {
        await Persist(customer?.Id);
        await inner.ResetCheckoutData(customer, storeId, clearCouponCodes, clearCheckoutAttributes,
            clearLoyaltyPoints, clearShipping, clearPayment);
    }

    public async Task InsertCustomerGroupInCustomer(CustomerGroup customerGroup, string customerId)
    {
        await Persist(customerId);
        await inner.InsertCustomerGroupInCustomer(customerGroup, customerId);
    }

    public async Task DeleteCustomerGroupInCustomer(CustomerGroup customerGroup, string customerId)
    {
        await Persist(customerId);
        await inner.DeleteCustomerGroupInCustomer(customerGroup, customerId);
    }

    public async Task DeleteAddress(Address address, string customerId)
    {
        await Persist(customerId);
        await inner.DeleteAddress(address, customerId);
    }

    public async Task InsertAddress(Address address, string customerId)
    {
        await Persist(customerId);
        await inner.InsertAddress(address, customerId);
    }

    public async Task UpdateAddress(Address address, string customerId)
    {
        await Persist(customerId);
        await inner.UpdateAddress(address, customerId);
    }

    public async Task UpdateBillingAddress(Address address, string customerId)
    {
        await Persist(customerId);
        await inner.UpdateBillingAddress(address, customerId);
    }

    public async Task UpdateShippingAddress(Address address, string customerId)
    {
        await Persist(customerId);
        await inner.UpdateShippingAddress(address, customerId);
    }

    public async Task ClearShoppingCartItem(string customerId, IList<ShoppingCartItem> cart)
    {
        await Persist(customerId);
        await inner.ClearShoppingCartItem(customerId, cart);
    }

    public async Task DeleteShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
    {
        await Persist(customerId);
        await inner.DeleteShoppingCartItem(customerId, shoppingCartItem);
    }

    public async Task InsertShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
    {
        await Persist(customerId);
        await inner.InsertShoppingCartItem(customerId, shoppingCartItem);
    }

    public async Task UpdateShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
    {
        await Persist(customerId);
        await inner.UpdateShoppingCartItem(customerId, shoppingCartItem);
    }

    #endregion
}
