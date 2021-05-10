using Grand.Domain.Customers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Interfaces
{
    public interface ICustomerNoteService
    {
        #region Customer note

        // <summary>
        /// Get note for customer
        /// </summary>
        /// <param name="id">Note identifier</param>
        /// <returns>CustomerNote</returns>
        Task<CustomerNote> GetCustomerNote(string id);

        /// <summary>
        /// Insert an customer note
        /// </summary>
        /// <param name="customerNote">The customer note</param>
        Task InsertCustomerNote(CustomerNote customerNote);

        /// <summary>
        /// Deletes an customer note
        /// </summary>
        /// <param name="customerNote">The customer note</param>
        Task DeleteCustomerNote(CustomerNote customerNote);

        /// <summary>
        /// Get notes for customer
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="displaytocustomer">Display to customer</param>
        /// <returns>OrderNote</returns>
        Task<IList<CustomerNote>> GetCustomerNotes(string customerId, bool? displaytocustomer = null);

        #endregion
    }
}
