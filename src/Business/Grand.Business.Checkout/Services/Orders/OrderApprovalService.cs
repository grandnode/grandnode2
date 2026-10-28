using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Order approval service
/// </summary>
public class OrderApprovalService : IOrderApprovalService
{
    #region Ctor

    public OrderApprovalService(IRepository<OrderApproval> repository,
        IOrderCalculationService orderCalculationService,
        CustomerSettings customerSettings,
        OrderApprovalSettings approvalSettings,
        IMediator mediator)
    {
        _repository = repository;
        _orderCalculationService = orderCalculationService;
        _customerSettings = customerSettings;
        _approvalSettings = approvalSettings;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<OrderApproval> _repository;
    private readonly IOrderCalculationService _orderCalculationService;
    private readonly CustomerSettings _customerSettings;
    private readonly OrderApprovalSettings _approvalSettings;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    public virtual async Task<OrderApproval> GetById(string id, string companyOwnerId)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(companyOwnerId))
            return null;

        return await _repository.FirstOrDefaultAsync(
            _repository.Table.Where(x => x.Id == id && x.CompanyOwnerId == companyOwnerId));
    }

    public virtual async Task<OrderApproval> GetByOrderId(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
            return null;

        return await _repository.FirstOrDefaultAsync(_repository.Table.Where(x => x.OrderId == orderId));
    }

    public virtual async Task<IPagedList<OrderApproval>> GetForCompany(string companyOwnerId, string storeId,
        int pageIndex, int pageSize)
    {
        // an empty id would match nothing meaningful, as in GetById
        if (string.IsNullOrEmpty(companyOwnerId) || string.IsNullOrEmpty(storeId))
            return new PagedList<OrderApproval>([], pageIndex, pageSize);

        var query = _repository.Table
            .Where(x => x.CompanyOwnerId == companyOwnerId && x.StoreId == storeId)
            .OrderBy(x => x.Status == OrderApprovalStatus.Pending ? 0 : 1)
            .ThenByDescending(x => x.CreatedOnUtc);

        return await _repository.PagedAsync(query, pageIndex, pageSize);
    }

    public virtual async Task<IPagedList<OrderApproval>> GetForCustomer(string customerId, string storeId,
        int pageIndex, int pageSize)
    {
        if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(storeId))
            return new PagedList<OrderApproval>([], pageIndex, pageSize);

        var query = _repository.Table
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId)
            .OrderByDescending(x => x.CreatedOnUtc);

        return await _repository.PagedAsync(query, pageIndex, pageSize);
    }

    public virtual async Task Insert(OrderApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);

        await _repository.InsertAsync(approval);
        await _mediator.EntityInserted(approval);
    }

    public virtual async Task Update(OrderApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);

        await _repository.UpdateAsync(approval);
        await _mediator.EntityUpdated(approval);
    }

    public virtual async Task<OrderApprovalCheckoutState> RequiresApproval(IList<ShoppingCartItem> cart,
        CalculationContext context)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(context);

        var customer = context.Customer;
        var limit = customer.OrderApprovalLimit;

        // not a limited buyer: nothing about approvals applies (also when the sub-account feature is off)
        if (_customerSettings.HideSubAccountsTab || string.IsNullOrEmpty(customer.OwnerId) ||
            OrderApprovalRules.CanApprove(customer) || !limit.HasValue)
            return new OrderApprovalCheckoutState(false, limit, null);

        var total = (await _orderCalculationService.GetShoppingCartTotal(cart, context))?.Total;

        // the total is only known once shipping is chosen; until then the rule is not evaluated
        return new OrderApprovalCheckoutState(total.HasValue && total.Value > limit.Value, limit, total);
    }

    public virtual bool IsPaymentMethodAllowed(string paymentMethodSystemName)
    {
        return !string.IsNullOrEmpty(paymentMethodSystemName) &&
               _approvalSettings.PaymentMethodSystemNames?.Contains(paymentMethodSystemName,
                   StringComparer.OrdinalIgnoreCase) == true;
    }

    #endregion
}
