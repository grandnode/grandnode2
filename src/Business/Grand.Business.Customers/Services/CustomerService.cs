using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Queries.Models;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Services
{
    /// <summary>
    /// Customer service
    /// </summary>
    public partial class CustomerService : ICustomerService
    {
        #region Fields

        private readonly IRepository<Customer> _customerRepository;
        private readonly IUserFieldService _userFieldService;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public CustomerService(
            IRepository<Customer> customerRepository,
            IUserFieldService userFieldService,
            IMediator mediator)
        {
            _customerRepository = customerRepository;
            _userFieldService = userFieldService;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        #region Customers

        /// <summary>
        /// Gets all customers
        /// </summary>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="affiliateId">Affiliate identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="ownerId">Owner identifier</param>
        /// <param name="salesEmployeeId">Sales employee identifier</param>
        /// <param name="customerGroupIds">A list of customer group identifiers to filter by (at least one match); pass null or empty list in order to load all customers; </param>
        /// <param name="email">Email; null to load all customers</param>
        /// <param name="username">Username; null to load all customers</param>
        /// <param name="firstName">First name; null to load all customers</param>
        /// <param name="lastName">Last name; null to load all customers</param>
        /// <param name="dayOfBirth">Day of birth; 0 to load all customers</param>
        /// <param name="monthOfBirth">Month of birth; 0 to load all customers</param>
        /// <param name="company">Company; null to load all customers</param>
        /// <param name="phone">Phone; null to load all customers</param>
        /// <param name="zipPostalCode">Phone; null to load all customers</param>
        /// <param name="loadOnlyWithShoppingCart">Value indicating whether to load customers only with shopping cart</param>
        /// <param name="sct">Value indicating what shopping cart type to filter; userd when 'loadOnlyWithShoppingCart' param is 'true'</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Customers</returns>
        public virtual async Task<IPagedList<Customer>> GetAllCustomers(DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null, string affiliateId = "", string vendorId = "", string storeId = "", string ownerId = "",
            string salesEmployeeId = "", string[] customerGroupIds = null, string[] customerTagIds = null, string email = null, string username = null,
            string firstName = null, string lastName = null,
            string company = null, string phone = null, string zipPostalCode = null,
            bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null,
            int pageIndex = 0, int pageSize = 2147483647, Expression<Func<Customer, object>> orderBySelector = null)
        {
            var querymodel = new GetCustomerQuery() {
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
            var query = await _mediator.Send(querymodel);
            return await PagedList<Customer>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets all customers by customer format (including deleted ones)
        /// </summary>
        /// <param name="passwordFormat">Password format</param>
        /// <returns>Customers</returns>
        public virtual async Task<IList<Customer>> GetAllCustomersByPasswordFormat(PasswordFormat passwordFormat)
        {
            var query = from p in _customerRepository.Table
                        select p;

            query = query.Where(c => c.PasswordFormatId == passwordFormat);
            query = query.OrderByDescending(c => c.CreatedOnUtc);
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Gets online customers
        /// </summary>
        /// <param name="lastActivityFromUtc">Customer last activity date (from)</param>
        /// <param name="customerGroupIds">A list of customer group identifiers to filter by (at least one match); pass null or empty list in order to load all customers; </param>
        /// <param name="storeId">Store ident</param>
        /// <param name="salesEmployeeId">Sales employee ident</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>

        /// <returns>Customers</returns>
        public virtual async Task<IPagedList<Customer>> GetOnlineCustomers(DateTime lastActivityFromUtc,
            string[] customerGroupIds, string storeId = "", string salesEmployeeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _customerRepository.Table
                        select p;

            query = query.Where(c => lastActivityFromUtc <= c.LastActivityDateUtc);
            query = query.Where(c => !c.Deleted);

            if (customerGroupIds != null && customerGroupIds.Length > 0)
                query = query.Where(c => c.Groups.Select(cr => cr).Intersect(customerGroupIds).Any());

            if (!string.IsNullOrEmpty(storeId))
                query = query.Where(c => c.StoreId == storeId);

            if (!string.IsNullOrEmpty(salesEmployeeId))
                query = query.Where(c => c.SeId == salesEmployeeId);

            query = query.OrderByDescending(c => c.LastActivityDateUtc);
            return await PagedList<Customer>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Gets count online customers
        /// </summary>
        /// <param name="lastActivityFromUtc">Customer last activity date (from)</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="salesEmployeeId">Sales employee ident</param>
        /// <returns>Int</returns>
        public virtual async Task<int> GetCountOnlineShoppingCart(DateTime lastActivityFromUtc, string storeId = "", string salesEmployeeId = "")
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
        /// Gets a customer
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>A customer</returns>
        public virtual Task<Customer> GetCustomerById(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return Task.FromResult<Customer>(null);

            return _customerRepository.GetByIdAsync(customerId);
        }

        /// <summary>
        /// Get customers by identifiers
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
            var sortedCustomers = new List<Customer>();
            foreach (var id in customerIds)
            {
                var customer = customers.Find(x => x.Id == id);
                if (customer != null)
                    sortedCustomers.Add(customer);
            }
            return await Task.FromResult(sortedCustomers);
        }

        /// <summary>
        /// Gets a customer by GUID
        /// </summary>
        /// <param name="customerGuid">Customer GUID</param>
        /// <returns>A customer</returns>
        public virtual async Task<Customer> GetCustomerByGuid(Guid customerGuid)
        {
            return await Task.FromResult(_customerRepository.Table.Where(x => x.CustomerGuid == customerGuid).FirstOrDefault());
        }

        /// <summary>
        /// Get customer by email
        /// </summary>
        /// <param name="email">Email</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await Task.FromResult(_customerRepository.Table.Where(x => x.Email == email.ToLowerInvariant()).FirstOrDefault());
        }

        /// <summary>
        /// Get customer by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomerBySystemName(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
                return null;

            return await Task.FromResult(_customerRepository.Table.Where(x => x.SystemName == systemName).FirstOrDefault());
        }

        /// <summary>
        /// Get customer by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> GetCustomerByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return await Task.FromResult(_customerRepository.Table.Where(x => x.Username == username.ToLowerInvariant()).FirstOrDefault());
        }

        /// <summary>
        /// Insert a guest customer
        /// </summary>
        /// <returns>Customer</returns>
        public virtual async Task<Customer> InsertGuestCustomer(Store store)
        {
            var customer = new Customer {
                CustomerGuid = Guid.NewGuid(),
                Active = true,
                StoreId = store.Id,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };

            //add to 'Guests' group
            var guestGroup = await _mediator.Send(new GetGroupBySystemNameQuery() { SystemName = SystemCustomerGroupNames.Guests });
            if (guestGroup == null)
                throw new GrandException("'Guests' group could not be loaded");
            customer.Groups.Add(guestGroup.Id);

            await _customerRepository.InsertAsync(customer);

            //event notification
            await _mediator.EntityInserted(customer);

            return customer;
        }

        /// <summary>
        /// Insert a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task InsertCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (!string.IsNullOrEmpty(customer.Email))
                customer.Email = customer.Email.ToLowerInvariant();

            if (!string.IsNullOrEmpty(customer.Username))
                customer.Username = customer.Username.ToLowerInvariant();

            await _customerRepository.InsertAsync(customer);

            //event notification
            await _mediator.EntityInserted(customer);
        }

        /// <summary>
        /// Updates the customer field
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task UpdateCustomerField<T>(Customer customer,
            Expression<Func<Customer, T>> expression, T value)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            await UpdateCustomerField(customer.Id, expression, value);

        }

        /// <summary>
        /// Updates the customer field
        /// </summary>
        /// <param name="customerId">Customer ident</param>
        public virtual async Task UpdateCustomerField<T>(string customerId,
            Expression<Func<Customer, T>> expression, T value)
        {
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException("customerId");

            await _customerRepository.UpdateField<T>(customerId, expression, value);

        }
        /// <summary>
        /// Updates the customer
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task UpdateCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

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
        /// Delete a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="hard">Hard delete from database</param>
        public virtual async Task DeleteCustomer(Customer customer, bool hard = false)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (customer.IsSystemAccount)
                throw new GrandException(string.Format("System customer account ({0}) could not be deleted", customer.SystemName));

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
        /// Updates the customer - last activity date
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task UpdateCustomerLastLoginDate(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var update = UpdateBuilder<Customer>.Create()
                .Set(x => x.LastLoginDateUtc, customer.LastLoginDateUtc)
                .Set(x => x.FailedLoginAttempts, customer.FailedLoginAttempts)
                .Set(x => x.CannotLoginUntilDateUtc, customer.CannotLoginUntilDateUtc);

            await _customerRepository.UpdateOneAsync(x => x.Id == customer.Id, update);

        }

        /// <summary>
        /// Updates the customer - password
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task UpdateCustomerPassword(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            await UpdateCustomerField(customer.Id, x => x.Password, customer.Password);

            //event notification
            await _mediator.EntityUpdated(customer);

        }

        public virtual async Task UpdateCustomerinAdminPanel(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var update = UpdateBuilder<Customer>.Create()
                .Set(x => x.Active, customer.Active)
                .Set(x => x.AdminComment, customer.AdminComment)
                .Set(x => x.AffiliateId, customer.AffiliateId)
                .Set(x => x.IsSystemAccount, customer.IsSystemAccount)
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
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var update = UpdateBuilder<Customer>.Create()
                .Set(x => x.Active, customer.Active)
                .Set(x => x.StoreId, customer.StoreId);

            await _customerRepository.UpdateOneAsync(x => x.Id == customer.Id, update);

            //event notification
            await _mediator.EntityUpdated(customer);
        }

        public virtual async Task UpdateContributions(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            await UpdateCustomerField(customer.Id, x => x.HasContributions, true);

            //event notification
            await _mediator.EntityUpdated(customer);
        }

        /// <summary>
        /// Reset data required for checkout
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
            if (customer == null)
                throw new ArgumentNullException();

            //clear entered coupon codes
            if (clearCouponCodes)
            {
                await _userFieldService.SaveField<string>(customer, SystemCustomerFieldNames.DiscountCoupons, null);
                await _userFieldService.SaveField<string>(customer, SystemCustomerFieldNames.GiftVoucherCoupons, null);
            }

            //clear checkout attributes
            if (clearCheckoutAttributes)
            {
                await _userFieldService.SaveField<string>(customer, SystemCustomerFieldNames.CheckoutAttributes, null, storeId);
            }

            //clear loyalty points flag
            if (clearLoyaltyPoints)
            {
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.UseLoyaltyPointsDuringCheckout, false, storeId);
            }

            //clear selected shipping method
            if (clearShipping)
            {
                await _userFieldService.SaveField<ShippingOption>(customer, SystemCustomerFieldNames.SelectedShippingOption, null, storeId);
                await _userFieldService.SaveField<ShippingOption>(customer, SystemCustomerFieldNames.OfferedShippingOptions, null, storeId);
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.SelectedPickupPoint, "", storeId);
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.ShippingOptionAttributeDescription, "", storeId);
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.ShippingOptionAttribute, "", storeId);
            }

            //clear selected payment method
            if (clearPayment)
            {
                await _userFieldService.SaveField<string>(customer, SystemCustomerFieldNames.SelectedPaymentMethod, null, storeId);
                await _userFieldService.SaveField<string>(customer, SystemCustomerFieldNames.PaymentTransaction, null, storeId);
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.PaymentOptionAttribute, "", storeId);
            }
        }

        /// <summary>
        /// Delete guest customer records
        /// </summary>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="onlyWithoutShoppingCart">A value indicating whether to delete customers only without shopping cart</param>
        /// <returns>Number of deleted customers</returns>
        public virtual async Task<int> DeleteGuestCustomers(DateTime? createdFromUtc, DateTime? createdToUtc, bool onlyWithoutShoppingCart)
        {
            var guestGroup = await _mediator.Send(new GetGroupBySystemNameQuery() { SystemName = SystemCustomerGroupNames.Guests });
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

            var customers = await _customerRepository.DeleteAsync(query);

            return customers.Count();

        }

        #endregion

        #region Customer group in customer

        public virtual async Task DeleteCustomerGroupInCustomer(CustomerGroup customerGroup, string customerId)
        {
            if (customerGroup == null)
                throw new ArgumentNullException(nameof(customerGroup));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException(nameof(customerId));

            await _customerRepository.Pull(customerId, x => x.Groups, customerGroup.Id);
        }

        public virtual async Task InsertCustomerGroupInCustomer(CustomerGroup customerGroup, string customerId)
        {
            if (customerGroup == null)
                throw new ArgumentNullException(nameof(customerGroup));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException(nameof(customerId));

            await _customerRepository.AddToSet(customerId, x => x.Groups, customerGroup.Id);

        }

        #endregion

        #region Customer Address

        public virtual async Task DeleteAddress(Address address, string customerId)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException(nameof(customerId));

            await _customerRepository.PullFilter(customerId, x => x.Addresses, x => x.Id, address.Id);

            //event notification
            await _mediator.EntityDeleted(address);

        }

        public virtual async Task InsertAddress(Address address, string customerId)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

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
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException(nameof(customerId));

            await _customerRepository.UpdateToSet(customerId, x => x.Addresses, z => z.Id, address.Id, address);

            //event notification
            await _mediator.EntityUpdated(address);
        }


        public virtual async Task UpdateBillingAddress(Address address, string customerId)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException(nameof(customerId));

            await _customerRepository.UpdateField(customerId, x => x.BillingAddress, address);

        }
        public virtual async Task UpdateShippingAddress(Address address, string customerId)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentNullException(nameof(customerId));

            await _customerRepository.UpdateField(customerId, x => x.ShippingAddress, address);
        }

        public virtual async Task RemoveShippingAddress(string customerId)
        {
            await _customerRepository.UpdateField(customerId, x => x.ShippingAddress, null);
        }

        #endregion

        #region Customer Shopping Cart Item

        public virtual async Task DeleteShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            await _customerRepository.PullFilter(customerId, x => x.ShoppingCartItems, x => x.Id, shoppingCartItem.Id);

            if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
            else
                await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);

        }

        public virtual async Task ClearShoppingCartItem(string customerId, IList<ShoppingCartItem> cart)
        {
            foreach (var item in cart)
            {
                await _customerRepository.PullFilter(customerId, x => x.ShoppingCartItems, x => x.Id, item.Id);
            }

            if (cart.Any(c => c.ShoppingCartTypeId == ShoppingCartType.ShoppingCart || c.ShoppingCartTypeId == ShoppingCartType.Auctions))
                await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
            if (cart.Any(c => c.ShoppingCartTypeId == ShoppingCartType.Wishlist))
                await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);
        }

        public virtual async Task InsertShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            await _customerRepository.AddToSet(customerId, x => x.ShoppingCartItems, shoppingCartItem);

            if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
            else
                await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);
        }

        public virtual async Task UpdateShoppingCartItem(string customerId, ShoppingCartItem shoppingCartItem)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException(nameof(shoppingCartItem));

            await _customerRepository.UpdateToSet(customerId, x => x.ShoppingCartItems, z => z.Id, shoppingCartItem.Id, shoppingCartItem);

            if (shoppingCartItem.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
                await UpdateCustomerField(customerId, x => x.LastUpdateCartDateUtc, DateTime.UtcNow);
            else
                await UpdateCustomerField(customerId, x => x.LastUpdateWishListDateUtc, DateTime.UtcNow);

        }

        #endregion

        #endregion
    }
}