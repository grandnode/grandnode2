using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
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
        OrderApprovalSettings approvalSettings,
        IMediator mediator)
    {
        _repository = repository;
        _approvalSettings = approvalSettings;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IRepository<OrderApproval> _repository;
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

    public virtual bool IsPaymentMethodAllowed(string paymentMethodSystemName)
    {
        return !string.IsNullOrEmpty(paymentMethodSystemName) &&
               _approvalSettings.PaymentMethodSystemNames?.Contains(paymentMethodSystemName,
                   StringComparer.OrdinalIgnoreCase) == true;
    }

    #endregion
}
