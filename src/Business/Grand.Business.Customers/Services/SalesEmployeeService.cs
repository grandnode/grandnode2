using Grand.Business.Customers.Interfaces;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Services
{
    public class SalesEmployeeService : ISalesEmployeeService
    {
        #region Fields

        private readonly IRepository<SalesEmployee> _salesEmployeeRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        public SalesEmployeeService(
            IRepository<SalesEmployee> salesEmployeeRepository,
            IMediator mediator,
            ICacheBase cacheBase)
        {
            _salesEmployeeRepository = salesEmployeeRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        /// <summary>
        /// Gets a sales employee
        /// </summary>
        /// <param name="salesEmployeeId">The sales employee identifier</param>
        /// <returns>SalesEmployee</returns>
        public virtual Task<SalesEmployee> GetSalesEmployeeById(string salesEmployeeId)
        {
            string key = string.Format(CacheKey.SALESEMPLOYEE_BY_ID_KEY, salesEmployeeId);
            return _cacheBase.GetAsync(key, () => _salesEmployeeRepository.GetByIdAsync(salesEmployeeId));
        }

        /// <summary>
        /// Gets all sales employees
        /// </summary>
        /// <returns>Warehouses</returns>
        public virtual async Task<IList<SalesEmployee>> GetAll()
        {
            return await _cacheBase.GetAsync(CacheKey.SALESEMPLOYEE_ALL, async () =>
            {
                var query = from se in _salesEmployeeRepository.Table
                            orderby se.DisplayOrder
                            select se;
                return await Task.FromResult(query.ToList());
            });
        }

        /// <summary>
        /// Inserts a sales employee
        /// </summary>
        /// <param name="salesEmployee">Sales Employee</param>
        public virtual async Task InsertSalesEmployee(SalesEmployee salesEmployee)
        {
            if (salesEmployee == null)
                throw new ArgumentNullException(nameof(salesEmployee));

            await _salesEmployeeRepository.InsertAsync(salesEmployee);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(salesEmployee);
        }

        /// <summary>
        /// Updates the sales employee
        /// </summary>
        /// <param name="salesEmployee">Sales Employee</param>
        public virtual async Task UpdateSalesEmployee(SalesEmployee salesEmployee)
        {
            if (salesEmployee == null)
                throw new ArgumentNullException(nameof(salesEmployee));

            await _salesEmployeeRepository.UpdateAsync(salesEmployee);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(salesEmployee);
        }

        /// <summary>
        /// Deletes a sales employee
        /// </summary>
        /// <param name="warehouse">The sales employee</param>
        public virtual async Task DeleteSalesEmployee(SalesEmployee salesEmployee)
        {
            if (salesEmployee == null)
                throw new ArgumentNullException(nameof(salesEmployee));

            await _salesEmployeeRepository.DeleteAsync(salesEmployee);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.SALESEMPLOYEE_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(salesEmployee);
        }
    }
}
