using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Common.Services.Directory
{
    public class GroupService : IGroupService
    {
        private readonly IRepository<CustomerGroup> _customerGroupRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #region Utilities

        /// <summary>
        /// Gets a value indicating whether customer is in a certain customer group
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="customerGroupSystemName">Customer group system name</param>
        /// <param name="onlyActiveCustomerGroups">A value indicating whether we should look only in active customer groups</param>
        /// <param name="isSystem">A value indicating whether we should look only in system groups</param>
        /// <returns>Result</returns>
        private async Task<bool> IsInCustomerGroup(Customer customer,
            string customerGroupSystemName,
            bool onlyActiveCustomerGroups = true,
            bool? isSystem = null)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (string.IsNullOrEmpty(customerGroupSystemName))
                throw new ArgumentNullException(nameof(customerGroupSystemName));

            var customerGroup = await GetCustomerGroupBySystemName(customerGroupSystemName);
            if (customerGroup == null)
                return false;

            var result =
                customer.Groups.Contains(customerGroup.Id)
                && (!onlyActiveCustomerGroups || customerGroup.Active)
                && (customerGroup.SystemName == customerGroupSystemName)
                && (!isSystem.HasValue || customerGroup.IsSystem == isSystem);

            return result;
        }

        #endregion

        public GroupService(IRepository<CustomerGroup> customerGroupRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _customerGroupRepository = customerGroupRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        /// <summary>
        /// Gets a customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group identifier</param>
        /// <returns>Customer group</returns>
        public virtual Task<CustomerGroup> GetCustomerGroupById(string customerGroupId)
        {
            if (string.IsNullOrWhiteSpace(customerGroupId))
                return Task.FromResult<CustomerGroup>(null);

            string key = string.Format(CacheKey.CUSTOMERGROUPS_BY_KEY, customerGroupId);
            return _cacheBase.GetAsync(key, () =>
            {
                return _customerGroupRepository.GetByIdAsync(customerGroupId);
            });


        }

        /// <summary>
        /// Gets a customer group
        /// </summary>
        /// <param name="systemName">Customer group system name</param>
        /// <returns>Customer group</returns>
        public virtual async Task<CustomerGroup> GetCustomerGroupBySystemName(string systemName)
        {
            string key = string.Format(CacheKey.CUSTOMERGROUPS_BY_SYSTEMNAME_KEY, systemName);
            return await _cacheBase.GetAsync(key, async () =>
            {
                return await Task.FromResult(_customerGroupRepository.Table.Where(x => x.SystemName == systemName).FirstOrDefault());
            });
        }

        /// <summary>
        /// Gets all customer groups
        /// </summary>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Customer groups</returns>
        public virtual async Task<IPagedList<CustomerGroup>> GetAllCustomerGroups(string name = "", int pageIndex = 0,
            int pageSize = int.MaxValue, bool showHidden = false)
        {
            var query = from m in _customerGroupRepository.Table
                        select m;

            if (!showHidden)
                query = query.Where(m => m.Active);
           
            if (!string.IsNullOrEmpty(name))
                query = query.Where(x => x.Name.ToLowerInvariant().Contains(name.ToLowerInvariant()));

            query = query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name);

            return await PagedList<CustomerGroup>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Inserts a customer group
        /// </summary>
        /// <param name="customerGroup">Customer group</param>
        public virtual async Task InsertCustomerGroup(CustomerGroup customerGroup)
        {
            if (customerGroup == null)
                throw new ArgumentNullException(nameof(customerGroup));

            await _customerGroupRepository.InsertAsync(customerGroup);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERGROUPS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(customerGroup);
        }

        /// <summary>
        /// Updates the customer group
        /// </summary>
        /// <param name="customerGroup">Customer group</param>
        public virtual async Task UpdateCustomerGroup(CustomerGroup customerGroup)
        {
            if (customerGroup == null)
                throw new ArgumentNullException(nameof(customerGroup));

            await _customerGroupRepository.UpdateAsync(customerGroup);

            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERGROUPS_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(customerGroup);
        }

        /// <summary>
        /// Delete a customer group
        /// </summary>
        /// <param name="customerGroup">Customer group</param>
        public virtual async Task DeleteCustomerGroup(CustomerGroup customerGroup)
        {
            if (customerGroup == null)
                throw new ArgumentNullException(nameof(customerGroup));

            if (customerGroup.IsSystem)
                throw new GrandException("System group could not be deleted");

            await _customerGroupRepository.DeleteAsync(customerGroup);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.CUSTOMERGROUPS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(customerGroup);
        }

        public Task<bool> IsStaff(Customer customer)
        {
            return IsInCustomerGroup(customer, SystemCustomerGroupNames.Staff, true, true);
        }

        public Task<bool> IsAdmin(Customer customer)
        {
            return IsInCustomerGroup(customer, SystemCustomerGroupNames.Administrators, true, true);
        }

        public Task<bool> IsSalesManager(Customer customer)
        {
            return IsInCustomerGroup(customer, SystemCustomerGroupNames.SalesManager, true, true);
        }

        public Task<bool> IsVendor(Customer customer)
        {
            return IsInCustomerGroup(customer, SystemCustomerGroupNames.Vendors, true, true);
        }

        public async Task<bool> IsOwner(Customer customer)
        {
            return await Task.FromResult(string.IsNullOrEmpty(customer.OwnerId));
        }

        public Task<bool> IsGuest(Customer customer)
        {
            return IsInCustomerGroup(customer, SystemCustomerGroupNames.Guests, true, true);
        }

        public Task<bool> IsRegistered(Customer customer)
        {
            return IsInCustomerGroup(customer, SystemCustomerGroupNames.Registered, true, true);
        }

        public async Task<IList<CustomerGroup>> GetAllByIds(string[] ids)
        {
            var customerGroups = await _cacheBase.GetAsync(CacheKey.CUSTOMERGROUPS_ALL, async () =>
            {
                var query = from cr in _customerGroupRepository.Table
                            orderby cr.Name
                            select cr;
                return await Task.FromResult(query.ToList());
            });
            return customerGroups.Where(x => ids.Contains(x.Id)).ToList();
        }
    }
}
