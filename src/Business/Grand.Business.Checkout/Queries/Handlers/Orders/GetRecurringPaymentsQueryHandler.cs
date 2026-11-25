using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class GetRecurringPaymentsQueryHandler : IRequestHandler<GetRecurringPaymentsQuery, IPagedList<RecurringPayment>>
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<RecurringPayment> _recurringPaymentRepository;

    public GetRecurringPaymentsQueryHandler(IRepository<RecurringPayment> recurringPaymentRepository,
        IRepository<Customer> customerRepository, IRepository<Order> orderRepository)
    {
        _recurringPaymentRepository = recurringPaymentRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    public async Task<IPagedList<RecurringPayment>> Handle(GetRecurringPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = from p in _recurringPaymentRepository.Table
            select p;

        if (!string.IsNullOrEmpty(request.StoreId))
            query = query.Where(x => x.StoreId == request.StoreId);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        //the recurring payment keeps the customer only, and with per-store identity one e-mail may have an account
        //in every store
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            var email = request.CustomerEmail.Trim().ToLowerInvariant();
            var customerIds = await _customerRepository.ToListAsync(
                _customerRepository.Table.Where(c => c.Email == email).Select(c => c.Id), cancellationToken);
            query = query.Where(x => x.CustomerId != null && customerIds.Contains(x.CustomerId));
        }

        //the sales employee is on the order, the same one the order list narrows by
        if (!string.IsNullOrEmpty(request.SalesEmployeeId))
        {
            var orderIds = await _orderRepository.ToListAsync(
                _orderRepository.Table.Where(o => o.SeId == request.SalesEmployeeId && o.RecurringCycleNumber == 1)
                    .Select(o => o.Id), cancellationToken);
            query = query.Where(x => x.InitialOrderId != null && orderIds.Contains(x.InitialOrderId));
        }

        query = query.OrderByDescending(x => x.CreatedOnUtc);

        return await _recurringPaymentRepository.PagedAsync(query, request.PageIndex, request.PageSize,
            cancellationToken);
    }
}
