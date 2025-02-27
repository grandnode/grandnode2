using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Customers;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using Grand.SharedKernel.Extensions;
using MediatR;
using System.Linq.Expressions;

namespace Grand.Business.Customers.Services;

/// <summary>
///     Customer service
/// </summary>
public class CustomerService : ICustomerService
{
    #region Ctor

    public CustomerService(
        IRepository<Customer> customerRepository,
        IMediator mediator,
        ICacheBase cacheBase)
    {
        _customerRepository = customerRepository;
        _mediator = mediator;
        _cacheBase = cacheBase;
    }

    #endregion

    #region Fields

    private readonly IRepository<Customer> _customerRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    #endregion

    #region Customers

    /// <summary>
    ///     Gets all customers
    /// </summary>
    /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
    /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
    /// <param name="affiliateId">Affiliate identifier</param>
    /// <param name="vendorId">Vendor identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="ownerId">Owner identifier</param>
    /// <param name="salesEmployeeId">Sales employee identifier</param>
    /// <param name="customerGroupIds">
    ///     A list of customer group identifiers to filter by (at least one match); pass null or
    ///     empty list in order to load all customers;
    /// </param>
    /// <param name="customerTagIds">customer tags ids</param>
    /// <param name="email">Email; null to load all customers</param>
    /// <param name="username">Username; null to load all customers</param>
    /// <param name="firstName">First name; null to load all customers</param>
    /// <param name="lastName">Last name; null to load all customers</param>
    /// <param name="company">Company; null to load all customers</param>
    /// <param name="phone">Phone; null to load all customers</param>
    /// <param name="zipPostalCode">Phone; null to load all customers</param>
    /// <param name="loadOnlyWithShoppingCart">Value indicating whether to load customers only with shopping cart</param>
    /// <param name="sct">
    ///     Value indicating what shopping cart type to filter; used when 'loadOnlyWithShoppingCart' param is
    ///     'true'
    /// </param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="orderBySelector">order by selector</param>
    /// <returns>Customers</returns>
    public virtual async Task<IPagedList<Customer>> GetAllCustomers(DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null, string affiliateId = "", string vendorId = "", string storeId = "",
        string ownerId = "",
        string salesEmployeeId = "", string[] customerGroupIds = null, string[] customerTagIds = null,
        string email = null, string username = null,
        string firstName = null, string lastName = null,
        string company = null, string phone = null, string zipPostalCode = null,
        bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null,
        int pageIndex = 0, int pageSize = 2147483647, Expression<Func<Customer, object>> orderBySelector = null)
    {
        var queryModel = new GetCustomerQuery {
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc,
            AffiliateId = affiliateId,
            VendorId = vendorId,
            StoreId = storeId,
            OwnerId = ownerId,
            SalesEmployeeId = salesEmployeeId,
            CustomerGroupIds = customerGroupIds,
            CustomerTagIds = customerTagIds,
            Email = email,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            Company = company,
            Phone = phone,
            ZipPostalCode = zipPostalCode,
            LoadOnlyWithShoppingCart = loadOnlyWithShoppingCart,
            Sct = sct,
            PageIndex = pageIndex,
            PageSize = pageSize,
            OrderBySelector = orderBySelector
        };
        var query = await _mediator.Send(queryModel);
        return await PagedList<Customer>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets online customers
    /// </summary>
    /// <param name="lastActivityFromUtc">Customer last activity date (from)</param>
    /// <param name="customerGroupIds">
    ///     A list of customer group identifiers to filter by (at least one match); pass null or
    ///     empty list in order to load all customers;
    /// </param>
    /// <param name="storeId">Store ident</param>
    /// <param name="salesEmployeeId">Sales employee ident</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Customers</returns>
    public virtual async Task<IPagedList<Customer>> GetOnlineCustomers(DateTime lastActivityFromUtc,
        string[] customerGroupIds, string storeId = "", string salesEmployeeId = "", int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var query = from p in _customerRepository.Table
            select p;

        query = query.Where(c => lastActivityFromUtc <= c.LastActivityDateUtc);
        query = query.Where(c => !c.Deleted);

        if (customerGroupIds is { Length: > 0 })
            query = query.Where(c => c.Groups.Select(cr => cr).Intersect(customerGroupIds).Any());

        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(c => c.StoreId == storeId);

        if (!string.IsNullOrEmpty(salesEmployeeId))
            query = query.Where(c => c.SeId == salesEmployeeId);

        query = query.OrderByDescending(c => c.LastActivityDateUtc);
        return await PagedList<Customer>.Create(query, pageIndex, pageSize);
    }

    /// <summary>
    ///     Gets count online customers
    /// </summary>
    /// <param name="lastActivityFromUtc">Customer last activity date (from)</param>
    /// <param name="storeId">Store ident</param>
    /// <param name="salesEmployeeId">Sales employee ident</param>
    /// <returns>Int</returns>
    public virtual async Task<int> GetCountOnlineShoppingCart(DateTime lastActivityFromUtc, string storeId = "",
        string salesEmployeeId = "")
    {
        var query = from p in _customerRepository.Table
            select p;

        query = query.Where(c => c.Active);
        query = query.Where(c => lastActivityFromUtc <= c.LastUpdateCartDateUtc);
        query = query.Where(c => c.ShoppingCartItems.Any(y => y.ShoppingCartTypeId == ShoppingCartType.ShoppingCart));

        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(c => c.StoreId == storeId);

        if (!string.IsNullOrEmpty(salesEmployeeId))
            query = query.Where(c => c.SeId == salesEmployeeId);

        return await Task.FromResult(query.Count());
    }

    /// <summary>
    ///     Gets a customer
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <returns>A customer</returns>
    public virtual Task<Customer> GetCustomerById(string customerId)
    {
        return string.IsNullOrWhiteSpace(customerId)
            ? Task.FromResult<Customer>(null)
            : _customerRepository.GetByIdAsync(customerId);
    }

    /// <summary>
    ///     Get customers by identifiers
    /// </summary>
    /// <param name="customerIds">Customer identifiers</param>
    /// <returns>Customers</returns>
    public virtual async Task<IList<Customer>> GetCustomersByIds(string[] customerIds)
    {
        if (customerIds == null || customerIds.Length == 0)
            return new List<Customer>();

        var query = from c in _customerRepository.Table
            where customerIds.Contains(c.Id)
            select c;
        var customers = query.ToList();
        //sort by passed identifiers
        var sortedCustomers = customerIds.Select(id => customers.Find(x => x.Id == id))
            .Where(customer => customer != null).ToList();
        return await Task.FromResult(sortedCustomers);
    }

    /// <summary>
    ///     Gets a customer by GUID
    /// </summary>
    /// <param name="customerGuid">Customer GUID</param>
    /// <returns>A customer</returns>
    public virtual async Task<Customer> GetCustomerByGuid(Guid customerGuid)
    {
        return await _customerRepository.GetOneAsync(x => x.CustomerGuid == customerGuid);
    }

    /// <summary>
    ///     Get customer by email
    /// </summary>
    /// <param name="email">Email</param>
    /// <returns>Customer</returns>
    public virtual Task<Customer> GetCustomerByEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email) ? Task.FromResult<Customer>(null) : _customerRepository.GetOneAsync(x => x.Email == email.ToLowerInvariant());
    }

    /// <summary>
    ///     Get customer by system name
    /// </summary>
    /// <param name="systemName">System name</param>
    /// <returns>Customer</returns>
    public virtual Task<Customer> GetCustomerBySystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return Task.FromResult<Customer>(null);

        var key = string.Format(CacheKey.CUSTOMER_BY_SYSTEMNAME_BY_KEY, systemName);

        return _cacheBase.GetAsync(key, () => _customerRepository.GetOneAsync(x => x.SystemName == systemName));
    }

    /// <summary>
    ///     Get customer by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>Customer</returns>
    public virtual Task<Customer> GetCustomerByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Task.FromResult<Customer>(null);

        return _customerRepository.GetOneAsync(x => x.Username == username.ToLowerInvariant());
    }

    /// <summary>
    ///     Insert a guest customer
    /// </summary>
    /// <returns>Customer</returns>
    public virtual async Task<Customer> InsertGuestCustomer(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        //add to 'Guests' group
        var guestGroup = await _mediator.Send(new GetGroupBySystemNameQuery
            { SystemName = SystemCustomerGroupNames.Guests });
        if (guestGroup == null)
            throw new GrandException("'Guests' group could not be loaded");
        customer.Groups.Add(guestGroup.Id);

        await _customerRepository.InsertAsync(customer);

        //event notification
        await _mediator.EntityInserted(customer);

        return customer;
    }

    /// <summary>
    ///     Insert a customer
    /// </summary>
    /// <param name="customer">Customer</param>
    public virtual async Task InsertCustomer(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (!string.IsNullOrEmpty(customer.Email))
            customer.Email = customer.Email.ToLowerInvariant();

        if (!string.IsNullOrEmpty(customer.Username))
            customer.Username = customer.Username.ToLowerInvariant();

        await _customerRepository.InsertAsync(customer);

        //event notification
        await _mediator.EntityInserted(customer);
    }

    /// <summary>
    ///     Updates the customer field
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="expression">expression</param>
    /// <param name="value">value</param>
    public virtual async Task UpdateCustomerField<T>(Customer customer,
        Expression<Func<Customer, T>> expression, T value)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await UpdateCustomerField(customer.Id, expression, value);
    }

    /// <summary>
    ///     Updates the customer field
    /// </summary>
    /// <param name="customerId">Customer ident</param>
    /// <param name="expression">Expression</param>
    /// <param name="value">value</param>
    public virtual async Task UpdateCustomerField<T>(string customerId,
        Expression<Func<Customer, T>> expression, T value)
    {
        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.UpdateField(customerId, expression, value);
    }

    /// <summary>
    ///     Updates the customer
    /// </summary>
    /// <param name="customer">Customer</param>
    public virtual async Task UpdateCustomer(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.IsSystemAccount)
            throw new GrandException(
                $"System customer account ({(string.IsNullOrEmpty(customer.SystemName) ? customer.Email : customer.SystemName)}) could not be updated");

        var update = UpdateBuilder<Customer>.Create()
            .Set(x => x.Email, string.IsNullOrEmpty(customer.Email) ? "" : customer.Email.ToLowerInvariant())
            .Set(x => x.PasswordFormatId, customer.PasswordFormatId)
            .Set(x => x.PasswordSalt, customer.PasswordSalt)
            .Set(x => x.Active, customer.Active)
            .Set(x => x.StoreId, customer.StoreId)
            .Set(x => x.Password, customer.Password)
            .Set(x => x.PasswordChangeDateUtc, customer.PasswordChangeDateUtc)
            .Set(x => x.Username, string.IsNullOrEmpty(customer.Username) ? "" : customer.Username.ToLowerInvariant())
            .Set(x => x.Deleted, customer.Deleted);

        await _customerRepository.UpdateOneAsync(x => x.Id == customer.Id, update);

        //event notification
        await _mediator.EntityUpdated(customer);
    }

    /// <summary>
    ///     Save attribute value
    /// </summary>
    /// <typeparam name="TPropType">Property type</typeparam>
    /// <param name="customer"></param>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    /// <param name="storeId">Store identifier; pass "" if this attribute will be available for all stores</param>
    public virtual async Task UpdateUserField<TPropType>(Customer customer, string key, TPropType value, string storeId = "")
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(key);

        var props = customer.UserFields.Where(x => string.IsNullOrEmpty(storeId) || x.StoreId == storeId);

        var prop = props.FirstOrDefault(ga => ga.Key.Equals(key, StringComparison.OrdinalIgnoreCase)); //should be culture invariant

        var valueStr = CommonHelper.To<string>(value);

        if (prop != null)
        {
            if (string.IsNullOrWhiteSpace(valueStr))
            {
                //delete
                await _customerRepository.PullFilter(customer.Id, x => x.UserFields,y => y.Key == prop.Key && y.StoreId == storeId);
                customer.UserFields.Remove(prop);
            }
            else
            {
                //update
                prop.Value = valueStr;
                await _customerRepository.UpdateToSet(customer.Id, x => x.UserFields,y => y.Key == prop.Key && y.StoreId == storeId, prop);
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(valueStr))
            {
                prop = new UserField {
                    Key = key,
                    Value = valueStr,
                    StoreId = storeId
                };
                await _customerRepository.AddToSet(customer.Id, x => x.UserFields, prop);
                customer.UserFields.Add(prop);
            }
        }
    }

    /// <summary>
    ///     Delete a customer
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="hard">Hard delete from database</param>
    public virtual async Task DeleteCustomer(Customer customer, bool hard = false)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.IsSystemAccount)
            throw new GrandException(
                $"System customer account ({(string.IsNullOrEmpty(customer.SystemName) ? customer.Email : customer.SystemName)}) could not be deleted");

        customer.Deleted = true;
        customer.Email = $"DELETED_@{DateTime.UtcNow.Ticks}.COM";
        customer.Username = customer.Email;

        //delete address
        customer.Addresses.Clear();
        customer.BillingAddress = null;
        customer.ShippingAddress = null;
        //delete user fields
        customer.UserFields.Clear();
        //delete shopping cart
        customer.ShoppingCartItems.Clear();
        //delete customer groups
        customer.Groups.Clear();
        //clear customer tags
        customer.CustomerTags.Clear();
        //update customer
        await _customerRepository.UpdateAsync(customer);

        if (hard)
            await _customerRepository.DeleteAsync(customer);

        //event notification
        await _mediator.EntityDeleted(customer);
    }

    /// <summary>
    ///     Updates the customer - last activity date
    /// </summary>
    /// <param name="customer">Customer</param>
    public virtual async Task UpdateCustomerLastLoginDate(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var update = UpdateBuilder<Customer>.Create()
            .Set(x => x.LastLoginDateUtc, customer.LastLoginDateUtc)
            .Set(x => x.FailedLoginAttempts, customer.FailedLoginAttempts)
            .Set(x => x.CannotLoginUntilDateUtc, customer.CannotLoginUntilDateUtc);

        await _customerRepository.UpdateOneAsync(x => x.Id == customer.Id, update);
    }

    public virtual async Task UpdateCustomerInAdminPanel(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.IsSystemAccount)
            throw new GrandException(
                $"System customer account ({(string.IsNullOrEmpty(customer.SystemName) ? customer.Email : customer.SystemName)}) could not be updated");

        var update = UpdateBuilder<Customer>.Create()
            .Set(x => x.Active, customer.Active)
            .Set(x => x.AdminComment, customer.AdminComment)
            .Set(x => x.AffiliateId, customer.AffiliateId)
            .Set(x => x.Active, customer.Active)
            .Set(x => x.Email, string.IsNullOrEmpty(customer.Email) ? "" : customer.Email.ToLowerInvariant())
            .Set(x => x.IsTaxExempt, customer.IsTaxExempt)
            .Set(x => x.Password, customer.Password)
            .Set(x => x.Username, string.IsNullOrEmpty(customer.Username) ? "" : customer.Username.ToLowerInvariant())
            .Set(x => x.Groups, customer.Groups)
            .Set(x => x.Addresses, customer.Addresses)
            .Set(x => x.FreeShipping, customer.FreeShipping)
            .Set(x => x.VendorId, customer.VendorId)
            .Set(x => x.SeId, customer.SeId)
            .Set(x => x.OwnerId, customer.OwnerId)
            .Set(x => x.StaffStoreId, customer.StaffStoreId)
            .Set(x => x.Attributes, customer.Attributes);

        await _customerRepository.UpdateOneAsync(x => x.Id == customer.Id, update);
        //event notification
        await _mediator.EntityUpdated(customer);
    }


    public virtual async Task UpdateActive(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var update = UpdateBuilder<Customer>.Create()
            .Set(x => x.Active, customer.Active)
            .Set(x => x.StoreId, customer.StoreId);

        await _customerRepository.UpdateOneAsync(x => x.Id == customer.Id, update);

        //event notification
        await _mediator.EntityUpdated(customer);
    }

    public virtual async Task UpdateContributions(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await UpdateCustomerField(customer.Id, x => x.HasContributions, true);

        //event notification
        await _mediator.EntityUpdated(customer);
    }

    /// <summary>
    ///     Reset data required for checkout
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="clearCouponCodes">A value indicating whether to clear coupon code</param>
    /// <param name="clearCheckoutAttributes">A value indicating whether to clear selected checkout attributes</param>
    /// <param name="clearLoyaltyPoints">A value indicating whether to clear "Use loyalty points" flag</param>
    /// <param name="clearShipping">A value indicating whether to clear selected shipping method</param>
    /// <param name="clearPayment">A value indicating whether to clear selected payment method</param>
    public virtual async Task ResetCheckoutData(Customer customer, string storeId,
        bool clearCouponCodes = false, bool clearCheckoutAttributes = false,
        bool clearLoyaltyPoints = true, bool clearShipping = true, bool clearPayment = true)
    {
        ArgumentNullException.ThrowIfNull(customer);

        //clear entered coupon codes
        if (clearCouponCodes)
        {
            await UpdateUserField<string>(customer, SystemCustomerFieldNames.DiscountCoupons, null);
            await UpdateUserField<string>(customer, SystemCustomerFieldNames.GiftVoucherCoupons, null);
        }

        //clear checkout attributes
        if (clearCheckoutAttributes)
            await UpdateUserField<string>(customer, SystemCustomerFieldNames.CheckoutAttributes, null, storeId);

        //clear loyalty points flag
        if (clearLoyaltyPoints)
            await UpdateUserField(customer, SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout, false, storeId);

        //clear selected shipping method
        if (clearShipping)
        {
            await UpdateUserField<ShippingOption>(customer, SystemCustomerFieldNames.SelectedShippingOption, null, storeId);
            await UpdateUserField<ShippingOption>(customer, SystemCustomerFieldNames.OfferedShippingOptions, null, storeId);
            await UpdateUserField(customer, SystemCustomerFieldNames.SelectedPickupPoint, "", storeId);
            await UpdateUserField(customer, SystemCustomerFieldNames.ShippingOptionAttributeDescription, "", storeId);
            await UpdateUserField(customer, SystemCustomerFieldNames.ShippingOptionAttribute, "", storeId);
        }

        //clear selected payment method
        if (clearPayment)
        {
            await UpdateUserField<string>(customer, SystemCustomerFieldNames.SelectedPaymentMethod, null, storeId);
            await UpdateUserField<string>(customer, SystemCustomerFieldNames.PaymentTransaction, null, storeId);
            await UpdateUserField(customer, SystemCustomerFieldNames.PaymentOptionAttribute, "", storeId);
        }
    }

    /// <summary>
    ///     Delete guest customer records
    /// </summary>
    /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
    /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
    /// <param name="onlyWithoutShoppingCart">A value indicating whether to delete customers only without shopping cart</param>
    /// <returns>Number of deleted customers</returns>
    public virtual async Task<int> DeleteGuestCustomers(DateTime? createdFromUtc, DateTime? createdToUtc,
        bool onlyWithoutShoppingCart)
    {
        var guestGroup = await _mediator.Send(new GetGroupBySystemNameQuery
            { SystemName = SystemCustomerGroupNames.Guests });
        if (guestGroup == null)
            throw new GrandException("Guests group could not be loaded");

        var query = from p in _customerRepository.Table
            select p;

        query = query.Where(x => x.Groups.Contains(guestGroup.Id));

        if (createdFromUtc.HasValue)
            query = query.Where(x => x.LastActivityDateUtc > createdFromUtc.Value);
        if (createdToUtc.HasValue)
            query = query.Where(x => x.LastActivityDateUtc < createdToUtc.Value);
        if (onlyWithoutShoppingCart)
            query = query.Where(x => !x.ShoppingCartItems.Any());

        query = query.Where(x => !x.HasContributions);

        query = query.Where(x => !x.IsSystemAccount);

        var count = query.Count();
        await _customerRepository.DeleteAsync(query);
        return count;
    }

    #endregion

    #region Customer group in customer

    public virtual async Task DeleteCustomerGroupInCustomer(CustomerGroup customerGroup, string customerId)
    {
        ArgumentNullException.ThrowIfNull(customerGroup);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.Pull(customerId, x => x.Groups, customerGroup.Id);
    }

    public virtual async Task InsertCustomerGroupInCustomer(CustomerGroup customerGroup, string customerId)
    {
        ArgumentNullException.ThrowIfNull(customerGroup);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.AddToSet(customerId, x => x.Groups, customerGroup.Id);
    }

    #endregion

    #region Customer Address

    public virtual async Task DeleteAddress(Address address, string customerId)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.PullFilter(customerId, x => x.Addresses, x => x.Id, address.Id);

        //event notification
        await _mediator.EntityDeleted(address);
    }

    public virtual async Task InsertAddress(Address address, string customerId)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        if (address.StateProvinceId == "0")
            address.StateProvinceId = "";

        await _customerRepository.AddToSet(customerId, x => x.Addresses, address);

        //event notification
        await _mediator.EntityInserted(address);
    }

    public virtual async Task UpdateAddress(Address address, string customerId)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.UpdateToSet(customerId, x => x.Addresses, z => z.Id, address.Id, address);

        //event notification
        await _mediator.EntityUpdated(address);
    }


    public virtual async Task UpdateBillingAddress(Address address, string customerId)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.UpdateField(customerId, x => x.BillingAddress, address);
    }

    public virtual async Task UpdateShippingAddress(Address address, string customerId)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentNullException(nameof(customerId));

        await _customerRepository.UpdateField(customerId, x => x.ShippingAddress, address);
    }

    #endregion

    #region Customer Shopping Cart Item

    public virtual async Task DeleteShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);

        await _customerRepository.PullFilter(customerId, x => x.ShoppingCartItems, x => x.Id, shoppingCartItem.Id);

        if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
        else
            await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);
    }

    public virtual async Task ClearShoppingCartItem(string customerId, IList<ShoppingCartItem> cart)
    {
        foreach (var item in cart)
            await _customerRepository.PullFilter(customerId, x => x.ShoppingCartItems, x => x.Id, item.Id);

        if (cart.Any(c => c.ShoppingCartTypeId is ShoppingCartType.ShoppingCart or ShoppingCartType.Auctions))
            await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
        if (cart.Any(c => c.ShoppingCartTypeId == ShoppingCartType.Wishlist))
            await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);
    }

    public virtual async Task InsertShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);

        await _customerRepository.AddToSet(customerId, x => x.ShoppingCartItems, shoppingCartItem);

        if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
        else
            await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);
    }

    public virtual async Task UpdateShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);

        await _customerRepository.UpdateToSet(customerId, x => x.ShoppingCartItems, z => z.Id, shoppingCartItem.Id,
            shoppingCartItem);

        if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
        else
            await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);
    }

    #endregion
}