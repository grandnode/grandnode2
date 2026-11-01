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
            // a request of the store has no page on the storefront list; not "== Company", a document without the field would not match
            .Where(x => x.CustomerId == customerId && x.StoreId == storeId && x.Decider != OrderApprovalDecider.Store)
            .OrderByDescending(x => x.CreatedOnUtc);

        return await _repository.PagedAsync(query, pageIndex, pageSize);
    }

    public virtual async Task Insert(OrderApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);

        await _repository.InsertAsync(approval);
        await _mediator.EntityInserted(approval);
    }

    public virtual async Task<bool> Close(OrderApproval approval, OrderApprovalStatus status,
        string decidedByCustomerId = null, string decisionComment = null)
    {
        ArgumentNullException.ThrowIfNull(approval);

        var closeId = Guid.NewGuid().ToString("N");
        var decidedOnUtc = DateTime.UtcNow;
        await _repository.UpdateOneAsync(x => x.Id == approval.Id && x.Status == OrderApprovalStatus.Pending,
            UpdateBuilder<OrderApproval>.Create()
                .Set(x => x.Status, status)
                .Set(x => x.DecidedOnUtc, decidedOnUtc)
                .Set(x => x.DecidedByCustomerId, decidedByCustomerId)
                .Set(x => x.DecisionComment, decisionComment)
                .Set(x => x.CloseId, closeId));

        // the update reports nothing, so the record tells which of two concurrent callers won
        var current = await _repository.GetByIdAsync(approval.Id);
        if (current?.CloseId != closeId)
            return false;

        approval.Status = status;
        approval.DecidedOnUtc = decidedOnUtc;
        approval.DecidedByCustomerId = decidedByCustomerId;
        approval.DecisionComment = decisionComment;
        approval.CloseId = closeId;
        await _mediator.EntityUpdated(approval);
        return true;
    }

    public virtual async Task Reopen(OrderApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);

        await _repository.UpdateOneAsync(x => x.Id == approval.Id && x.CloseId == approval.CloseId,
            UpdateBuilder<OrderApproval>.Create()
                .Set(x => x.Status, OrderApprovalStatus.Pending)
                .Set(x => x.DecidedOnUtc, null)
                .Set(x => x.DecidedByCustomerId, null)
                .Set(x => x.DecisionComment, null)
                .Set(x => x.CloseId, null));

        approval.Status = OrderApprovalStatus.Pending;
        approval.DecidedOnUtc = null;
        approval.DecidedByCustomerId = null;
        approval.DecisionComment = null;
        approval.CloseId = null;
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
