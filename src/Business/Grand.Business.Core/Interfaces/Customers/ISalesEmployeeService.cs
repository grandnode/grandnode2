﻿using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Customers
{
    public interface ISalesEmployeeService
    {
        /// <summary>
        /// Gets a sales employee
        /// </summary>
        /// <param name="salesEmployeeId">The sales employee identifier</param>
        /// <returns>SalesEmployee</returns>
        Task<SalesEmployee> GetSalesEmployeeById(string salesEmployeeId);

        /// <summary>
        /// Gets all sales employees
        /// </summary>
        /// <returns>Warehouses</returns>
        Task<IList<SalesEmployee>> GetAll();

        /// <summary>
        /// Inserts a sales employee
        /// </summary>
        /// <param name="salesEmployee">Sales Employee</param>
        Task InsertSalesEmployee(SalesEmployee salesEmployee);

        /// <summary>
        /// Updates the sales employee
        /// </summary>
        /// <param name="salesEmployee">Sales Employee</param>
        Task UpdateSalesEmployee(SalesEmployee salesEmployee);

        /// <summary>
        /// Deletes a sales employee
        /// </summary>
        /// <param name="salesEmployee">The sales employee</param>
        Task DeleteSalesEmployee(SalesEmployee salesEmployee);

    }
}
