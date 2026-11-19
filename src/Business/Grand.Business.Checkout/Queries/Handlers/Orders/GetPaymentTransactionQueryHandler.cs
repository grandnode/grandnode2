using Grand.Business.Core.Extensions;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class
    GetPaymentTransactionQueryHandler : IRequestHandler<GetPaymentTransactionQuery, IPagedList<PaymentTransaction>>
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<PaymentTransaction> _paymentTransactionRepository;

    public GetPaymentTransactionQueryHandler(IRepository<PaymentTransaction> paymentTransactionRepository,
        IRepository<Order> orderRepository)
    {
        _paymentTransactionRepository = paymentTransactionRepository;
        _orderRepository = orderRepository;
    }

    public async Task<IPagedList<PaymentTransaction>> Handle(GetPaymentTransactionQuery request,
        CancellationToken cancellationToken)
    {
        var query = from p in _paymentTransactionRepository.Table
            select p;

        if (!string.IsNullOrEmpty(request.StoreId))
            query = query.Where(rr => request.StoreId == rr.StoreId);

        if (!string.IsNullOrEmpty(request.CustomerEmail))
            query = query.Where(rr => rr.CustomerEmail == request.CustomerEmail.ToLowerInvariant());

        if (request.OrderGuid.HasValue)
            query = query.Where(rr => rr.OrderGuid == request.OrderGuid.Value);

        if (request.Ts.HasValue)
            query = query.Where(rr => rr.TransactionStatus == request.Ts.Value);

        if (request.CreatedFromUtc.HasValue)
            query = query.Where(rr => request.CreatedFromUtc.Value <= rr.CreatedOnUtc);

        if (request.CreatedToUtc.HasValue)
            query = query.Where(rr => request.CreatedToUtc.Value >= rr.CreatedOnUtc);

        if (!request.IncludeTemp)
            query = query.Where(rr => !rr.Temp);

        if (!string.IsNullOrEmpty(request.PaymentMethodSystemName))
            query = query.Where(rr => rr.PaymentMethodSystemName == request.PaymentMethodSystemName);

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderNumber) || request.OverdueOnly)
        {
            var orderGuids = await MatchingOrderGuids(request, cancellationToken);
            query = query.Where(rr => orderGuids.Contains(rr.OrderGuid));
        }

        query = query.OrderByDescending(rr => rr.CreatedOnUtc);

        return await _paymentTransactionRepository.PagedAsync(query, request.PageIndex, request.PageSize, cancellationToken);
    }

    /// <summary>
    ///     The purchase order number and the due date live on the order, so the orders matching them are found first
    /// </summary>
    private Task<IList<Guid>> MatchingOrderGuids(GetPaymentTransactionQuery request,
        CancellationToken cancellationToken)
    {
        var orders = _orderRepository.Table.Where(o => !o.Deleted);

        if (!string.IsNullOrEmpty(request.StoreId))
            orders = orders.Where(o => o.StoreId == request.StoreId);

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderNumber))
        {
            var purchaseOrderNumber = request.PurchaseOrderNumber.Trim();
            orders = orders.Where(o => o.PurchaseOrderNumber == purchaseOrderNumber);
        }

        //the same rule as PaymentExtensions.IsPaymentOverdue
        if (request.OverdueOnly)
        {
            var today = DateTime.UtcNow.Date;
            orders = orders.Where(PaymentExtensions.UnpaidOnCredit).Where(o => o.PaymentDueDateUtc < today);
        }

        return _orderRepository.ToListAsync(orders.Select(o => o.OrderGuid), cancellationToken);
    }
}