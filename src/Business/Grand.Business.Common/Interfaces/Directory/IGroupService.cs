using Grand.Domain;
using Grand.Domain.Customers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Common.Interfaces.Directory
{
    public interface IGroupService
    {
        /// <summary>
        /// Gets a value indicating whether customer is staff
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsStaff(Customer customer);

        /// <summary>
        /// Gets a value indicating whether customer is administrator
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsAdmin(Customer customer);

        /// <summary>
        /// Gets a value indicating whether customer is sales manager
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsSalesManager(Customer customer);

        /// <summary>
        /// Gets a value indicating whether customer is vendor
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsVendor(Customer customer);

        /// <summary>
        /// Gets a value indicating whether customer is owner subaccount
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsOwner(Customer customer);

        /// <summary>
        /// Gets a value indicating whether customer is guest
        /// </summary>
        /// <param name="customer">Customer</param>        
        /// <returns>Result</returns>
        Task<bool> IsGuest(Customer customer);

        /// <summary>
        /// Gets a value indicating whether customer is registered
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <returns>Result</returns>
        Task<bool> IsRegistered(Customer customer);


        /// <summary>
        /// Inserts a customer group
        /// </summary>
        /// <param name="customerGroup">Customer group</param>
        Task InsertCustomerGroup(CustomerGroup customerGroup);

        /// <summary>
        /// Updates the customer group
        /// </summary>
        /// <param name="customerGroup">Customer group</param>
        Task UpdateCustomerGroup(CustomerGroup customerGroup);

        /// <summary>
        /// Delete a customer group
        /// </summary>
        /// <param name="customerGroup">Customer group</param>
        Task DeleteCustomerGroup(CustomerGroup customerGroup);

        /// <summary>
        /// Gets a customer group
        /// </summary>
        /// <param name="customerGroupId">Customer group identifier</param>
        /// <returns>Customer group</returns>
        Task<CustomerGroup> GetCustomerGroupById(string customerGroupId);

        /// <summary>
        /// Gets a customer group
        /// </summary>
        /// <param name="systemName">Customer group system name</param>
        /// <returns>Customer group</returns>
        Task<CustomerGroup> GetCustomerGroupBySystemName(string systemName);

        /// <summary>
        /// Gets all customer groups 
        /// </summary>
        /// <param name="ids">Group id's </param>
        /// <returns>Customer groups</returns>
        Task<IList<CustomerGroup>> GetAllByIds(string[] ids);

        /// <summary>
        /// Gets all customer groups
        /// </summary>
        /// <returns>Customer groups</returns>
        Task<IPagedList<CustomerGroup>> GetAllCustomerGroups(string name = "", int pageIndex = 0,
            int pageSize = int.MaxValue, bool showHidden = false);

    }
}
