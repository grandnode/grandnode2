using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Services
{
    public class CustomerNoteService : ICustomerNoteService
    {
        private readonly IRepository<CustomerNote> _customerNoteRepository;
        private readonly IMediator _mediator;

        public CustomerNoteService(IRepository<CustomerNote> customerNoteRepository,
            IMediator mediator)
        {
            _customerNoteRepository = customerNoteRepository;
            _mediator = mediator;
        }

        #region Customer note

        // <summary>
        /// Get note for customer
        /// </summary>
        /// <param name="id">Note identifier</param>
        /// <returns>CustomerNote</returns>
        public virtual Task<CustomerNote> GetCustomerNote(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult<CustomerNote>(null);

            return _customerNoteRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Insert an customer note
        /// </summary>
        /// <param name="customerNote">The customer note</param>
        public virtual async Task InsertCustomerNote(CustomerNote customerNote)
        {
            if (customerNote == null)
                throw new ArgumentNullException(nameof(customerNote));

            await _customerNoteRepository.InsertAsync(customerNote);

            //event notification
            await _mediator.EntityInserted(customerNote);
        }

        /// <summary>
        /// Deletes an customer note
        /// </summary>
        /// <param name="customerNote">The customer note</param>
        public virtual async Task DeleteCustomerNote(CustomerNote customerNote)
        {
            if (customerNote == null)
                throw new ArgumentNullException(nameof(customerNote));

            await _customerNoteRepository.DeleteAsync(customerNote);

            //event notification
            await _mediator.EntityDeleted(customerNote);
        }

        /// <summary>
        /// Get notes for customer
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="displaytocustomer">Display to customer</param>
        /// <returns>OrderNote</returns>
        public virtual async Task<IList<CustomerNote>> GetCustomerNotes(string customerId, bool? displaytocustomer = null)
        {
            var query = from customerNote in _customerNoteRepository.Table
                        where customerNote.CustomerId == customerId
                        select customerNote;

            if (displaytocustomer.HasValue)
                query = query.Where(x => x.DisplayToCustomer == displaytocustomer.Value);

            query = query.OrderByDescending(x => x.CreatedOnUtc);

            return await Task.FromResult(query.ToList());
        }

        #endregion


    }
}
