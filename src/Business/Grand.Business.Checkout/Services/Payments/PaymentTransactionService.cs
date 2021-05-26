using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Payments;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Payments
{
    public partial class PaymentTransactionService : IPaymentTransactionService
    {
        private readonly IRepository<PaymentTransaction> _repositoryPaymentTransaction;
        private readonly IMediator _mediator;

        public PaymentTransactionService(
            IRepository<PaymentTransaction> repositoryPaymentTransaction,
            IMediator mediator)
        {
            _repositoryPaymentTransaction = repositoryPaymentTransaction;
            _mediator = mediator;
        }
        /// <summary>
        /// Inserts an payment transaction
        /// </summary>
        /// <param name="paymentTransaction">payment transaction</param>
        public virtual async Task InsertPaymentTransaction(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            await _repositoryPaymentTransaction.InsertAsync(paymentTransaction);

            //event notification
            await _mediator.EntityInserted(paymentTransaction);

        }
        /// <summary>
        /// Update an payment transaction
        /// </summary>
        /// <param name="paymentTransaction">payment transaction</param>
        public virtual async Task UpdatePaymentTransaction(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            await _repositoryPaymentTransaction.UpdateAsync(paymentTransaction);

            //event notification
            await _mediator.EntityUpdated(paymentTransaction);

        }

        /// <summary>
        /// Delete an payment transaction
        /// </summary>
        /// <param name="paymentTransaction">payment transaction</param>
        public virtual async Task DeletePaymentTransaction(PaymentTransaction paymentTransaction)
        {
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(paymentTransaction));

            await _repositoryPaymentTransaction.DeleteAsync(paymentTransaction);

            //event notification
            await _mediator.EntityDeleted(paymentTransaction);

        }

        /// <summary>
        /// Gets an payment transaction
        /// </summary>
        /// <param name="id">The identifier</param>
        /// <returns>PaymentTransaction</returns>
        public virtual Task<PaymentTransaction> GetById(string id)
        {
            return _repositoryPaymentTransaction.GetByIdAsync(id);
        }

        /// <summary>
        /// Gets an payment transactions for order Ident 
        /// </summary>
        /// <param name="orderCode">The order code</param>
        /// <returns>PaymentTransaction</returns>
        public virtual async Task<IList<PaymentTransaction>> GetByOrderCode(string orderCode)
        {
            return await Task.FromResult(
                _repositoryPaymentTransaction.Table.Where(x => x.OrderCode == orderCode)
                .ToList());
        }

        /// <summary>
        /// Gets an payment transactions for order guid
        /// </summary>
        /// <param name="orderguid">The order guid</param>
        /// <returns>PaymentTransaction</returns>
        public virtual async Task<PaymentTransaction> GetByOrdeGuid(Guid orderguid)
        {
            return await Task.FromResult(_repositoryPaymentTransaction.Table.Where(x => x.OrderGuid == orderguid).FirstOrDefault());
        }

        /// <summary>
        /// Get an payment transactions by authorization transaction ID and payment method system name
        /// </summary>
        /// <param name="authorizationTransactionId">Authorization transaction ID</param>
        /// <param name="paymentMethodSystemName">Payment method system name</param>
        /// <returns>Order</returns>
        public virtual async Task<IList<PaymentTransaction>> GetByAuthorizationTransactionIdAndPaymentMethod(string authorizationTransactionId, string paymentMethodSystemName)
        {
            var query = from p in _repositoryPaymentTransaction.Table
                        select p;

            if (!string.IsNullOrEmpty(authorizationTransactionId))
                query = query.Where(c => c.AuthorizationTransactionId == authorizationTransactionId);

            if (!string.IsNullOrEmpty(paymentMethodSystemName))
                query = query.Where(c => c.PaymentMethodSystemName == paymentMethodSystemName);

            return await Task.FromResult(query.ToList());
        }

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
        public virtual async Task<IPagedList<PaymentTransaction>> SearchPaymentTransactions(
              Guid? orderguid = null,
              string storeId = "",
              string customerEmail = "",
              TransactionStatus? ts = null,
              int pageIndex = 0, int pageSize = int.MaxValue,
              DateTime? createdFromUtc = null,
              DateTime? createdToUtc = null)
        {
            var model = new GetPaymentTransactionQuery() {
                CreatedFromUtc = createdFromUtc,
                CreatedToUtc = createdToUtc,
                PageIndex = pageIndex,
                PageSize = pageSize,
                OrderGuid = orderguid,
                CustomerEmail = customerEmail,
                StoreId = storeId,
                Ts = ts,
            };

            var query = await _mediator.Send(model);
            return await PagedList<PaymentTransaction>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Set payment error for transaction
        /// </summary>
        public virtual async Task SetError(string paymenttransactionId, List<string> errors)
        {
            await _repositoryPaymentTransaction.UpdateField(paymenttransactionId, x => x.Errors, errors);
        }
    }
}
