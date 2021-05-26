using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Customers
{
    public partial class CustomerActionService : ICustomerActionService
    {
        #region Fields

        private readonly IRepository<CustomerAction> _customerActionRepository;
        private readonly IRepository<CustomerActionType> _customerActionTypeRepository;
        private readonly IRepository<CustomerActionHistory> _customerActionHistoryRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        public CustomerActionService(IRepository<CustomerAction> customerActionRepository,
            IRepository<CustomerActionType> customerActionTypeRepository,
            IRepository<CustomerActionHistory> customerActionHistoryRepository,
            IMediator mediator,
            ICacheBase cacheBase)
        {
            _customerActionRepository = customerActionRepository;
            _customerActionTypeRepository = customerActionTypeRepository;
            _customerActionHistoryRepository = customerActionHistoryRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets customer action
        /// </summary>
        /// <param name="id">Customer action identifier</param>
        /// <returns>Customer Action</returns>
        public virtual Task<CustomerAction> GetCustomerActionById(string id)
        {
            return _customerActionRepository.GetByIdAsync(id);
        }


        /// <summary>
        /// Gets all customer actions
        /// </summary>
        /// <returns>Customer actions</returns>
        public virtual async Task<IList<CustomerAction>> GetCustomerActions()
        {
            return await Task.FromResult(_customerActionRepository.Table.ToList());
        }

        /// <summary>
        /// Inserts a customer action
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        public virtual async Task InsertCustomerAction(CustomerAction customerAction)
        {
            if (customerAction == null)
                throw new ArgumentNullException(nameof(customerAction));

            await _customerActionRepository.InsertAsync(customerAction);

            //event notification
            await _mediator.EntityInserted(customerAction);

        }

        /// <summary>
        /// Delete a customer action
        /// </summary>
        /// <param name="customerAction">Customer action</param>
        public virtual async Task DeleteCustomerAction(CustomerAction customerAction)
        {
            if (customerAction == null)
                throw new ArgumentNullException(nameof(customerAction));

            await _customerActionRepository.DeleteAsync(customerAction);

            //event notification
            await _mediator.EntityDeleted(customerAction);

        }

        /// <summary>
        /// Updates the customer action
        /// </summary>
        /// <param name="customerTag">Customer tag</param>
        public virtual async Task UpdateCustomerAction(CustomerAction customerAction)
        {
            if (customerAction == null)
                throw new ArgumentNullException(nameof(customerAction));

            await _customerActionRepository.UpdateAsync(customerAction);

            //event notification
            await _mediator.EntityUpdated(customerAction);
        }

        #endregion

        #region Condition Type

        public virtual async Task<IList<CustomerActionType>> GetCustomerActionType()
        {
            return await Task.FromResult(_customerActionTypeRepository.Table.ToList());
        }

        public virtual async Task<IPagedList<CustomerActionHistory>> GetAllCustomerActionHistory(string customerActionId, int pageIndex = 0, int pageSize = 2147483647)
        {
            var query = from h in _customerActionHistoryRepository.Table
                        where h.CustomerActionId == customerActionId
                        select h;
            return await PagedList<CustomerActionHistory>.Create(query, pageIndex, pageSize);
        }

        public virtual async Task<CustomerActionType> GetCustomerActionTypeById(string id)
        {
            return await _customerActionTypeRepository.GetByIdAsync(id);
        }

        public virtual async Task UpdateCustomerActionType(CustomerActionType customerActionType)
        {
            if (customerActionType == null)
                throw new ArgumentNullException(nameof(customerActionType));

            await _customerActionTypeRepository.UpdateAsync(customerActionType);

            //clear cache
            await _cacheBase.RemoveAsync(CacheKey.CUSTOMER_ACTION_TYPE);
            //event notification
            await _mediator.EntityUpdated(customerActionType);
        }

        #endregion

    }
}
