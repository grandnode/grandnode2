using Grand.Domain;
using Grand.Domain.Payments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Payments
{
    public interface IPaymentTransactionService
    {
        /// <summary>
        /// Inserts an payment transaction
        /// </summary>
        /// <param name="paymentTransaction">payment transaction</param>
        Task InsertPaymentTransaction(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Update an payment transaction
        /// </summary>
        /// <param name="paymentTransaction">payment transaction</param>
        Task UpdatePaymentTransaction(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Delete an payment transaction
        /// </summary>
        /// <param name="paymentTransaction">payment transaction</param>
        Task DeletePaymentTransaction(PaymentTransaction paymentTransaction);

        /// <summary>
        /// Gets an payment transaction
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>PaymentTransaction</returns>
        Task<PaymentTransaction> GetById(string id);

        /// <summary>
        /// Gets an payment transactions for order Ident 
        /// </summary>
        /// <param name="orderCode">The order code</param>
        /// <returns>PaymentTransaction</returns>
        Task<IList<PaymentTransaction>> GetByOrderCode(string orderCode);

        /// <summary>
        /// Gets an payment transactions for order guid
        /// </summary>
        /// <param name="orderguid">The order guid</param>
        /// <returns>PaymentTransaction</returns>
        Task<PaymentTransaction> GetByOrdeGuid(Guid orderguid);

        /// <summary>
        /// Get an payment transactions by authorization transaction ID and payment method system name
        /// </summary>
        /// <param name="authorizationTransactionId">Authorization transaction ID</param>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>Order</returns>
        Task<IList<PaymentTransaction>> GetByAuthorizationTransactionIdAndPaymentMethod(string authorizationTransactionId, string paymentMethodSystemName);

        /// <summary>
        /// Search payment transactions
        /// </summary>
        /// <param name="orderguid">Order ident by guid</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="customerEmail">Customer email</param>
        /// <param name="ts">Transaction status</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>        
        /// <returns>Page list payment transaction</returns>
        Task<IPagedList<PaymentTransaction>> SearchPaymentTransactions(
            Guid? orderguid = null,
            string storeId = "", 
            string customerEmail = "",
            TransactionStatus? ts = null,
            int pageIndex = 0, int pageSize = int.MaxValue, 
            DateTime? createdFromUtc = null, 
            DateTime? createdToUtc = null);

        /// <summary>
        /// Set payment error for transaction
        /// </summary>
        Task SetError(string paymentTransactionId, List<string> errors);
    }
}
