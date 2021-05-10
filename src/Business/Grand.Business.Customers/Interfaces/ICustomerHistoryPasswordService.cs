using Grand.Domain.Customers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Interfaces
{
    public interface ICustomerHistoryPasswordService
    {
        #region Password history

        /// <summary>
        /// Gets customer passwords
        /// </summary>
        /// <param name="customerId">Customer identifier; pass null to load all records</param>
        /// <param name="passwordsToReturn">Number of returning passwords; pass null to load all records</param>
        /// <returns>List of customer passwords</returns>
        Task<IList<CustomerHistoryPassword>> GetPasswords(string customerId, int passwordsToReturn);

        /// <summary>
        /// Insert a customer history password
        /// </summary>
        /// <param name="customer">Customer</param>
        Task InsertCustomerPassword(Customer customer);


        #endregion
    }
}
