﻿using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class GetPaymentTransactionQuery : IRequest<IQueryable<PaymentTransaction>>
    {
        public string StoreId { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public Guid? OrderGuid { get; set; }
        public TransactionStatus? Ts { get; set; } = null;
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = int.MaxValue;
        public DateTime? CreatedFromUtc { get; set; } = null;
        public DateTime? CreatedToUtc { get; set; } = null;
    }
}
