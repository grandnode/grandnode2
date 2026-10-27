using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Handlers.Orders;

public class GetOrderApprovalsHandler : IRequestHandler<GetOrderApprovals, OrderApprovalListModel>
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEnumTranslationService _enumTranslationService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderService _orderService;

    public GetOrderApprovalsHandler(IOrderApprovalService orderApprovalService, ICustomerService customerService,
        IOrderService orderService, IDateTimeService dateTimeService, IEnumTranslationService enumTranslationService)
    {
        _orderApprovalService = orderApprovalService;
        _customerService = customerService;
        _orderService = orderService;
        _dateTimeService = dateTimeService;
        _enumTranslationService = enumTranslationService;
    }

    public async Task<OrderApprovalListModel> Handle(GetOrderApprovals request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        var pageIndex = Math.Max(request.Command.PageNumber, 1) - 1;
        var pageSize = request.Command.PageSize <= 0 ? DefaultPageSize : Math.Min(request.Command.PageSize, MaxPageSize);

        // approvers (the owner and approver sub-accounts) see the company's requests, buyers only their own
        var isCompanyList = OrderApprovalRules.CanApprove(request.Customer);
        var approvals = isCompanyList
            ? await _orderApprovalService.GetForCompany(OrderApprovalRules.CompanyId(request.Customer),
                request.Store.Id, pageIndex, pageSize)
            : await _orderApprovalService.GetForCustomer(request.Customer.Id, request.Store.Id, pageIndex, pageSize);

        var model = new OrderApprovalListModel { IsCompanyList = isCompanyList };
        model.PagingContext.LoadPagedList(approvals);

        var buyers = new Dictionary<string, Customer>();
        if (isCompanyList && approvals.Count > 0)
            foreach (var buyer in await _customerService.GetCustomersByIds(
                         approvals.Select(x => x.CustomerId).Distinct().ToArray()))
                buyers[buyer.Id] = buyer;

        var orders = new Dictionary<string, Order>();
        var orderIds = approvals.Where(x => !string.IsNullOrEmpty(x.OrderId)).Select(x => x.OrderId).Distinct()
            .ToArray();
        if (orderIds.Length > 0)
            foreach (var order in await _orderService.GetOrdersByIds(orderIds))
                orders[order.Id] = order;

        foreach (var approval in approvals)
        {
            var order = approval.OrderId != null ? orders.GetValueOrDefault(approval.OrderId) : null;
            model.Approvals.Add(new OrderApprovalListModel.OrderApprovalRowModel {
                Id = approval.Id,
                CreatedOn = _dateTimeService.ConvertToUserTime(approval.CreatedOnUtc, DateTimeKind.Utc),
                StatusId = (int)approval.Status,
                Status = _enumTranslationService.GetTranslationEnum(approval.Status),
                IsPending = approval.Status == OrderApprovalStatus.Pending,
                Buyer = isCompanyList
                    ? OrderApprovalBuyerName.For(buyers.GetValueOrDefault(approval.CustomerId))
                    : null,
                OrderId = order?.Id,
                OrderNumber = order?.OrderNumber ?? 0,
                CanViewOrder = order != null && OrderApprovalRules.CanOpenOrder(request.Customer, order),
                ItemCount = approval.Items.Count,
                Total = approval.Summary?.Total
            });
        }

        return model;
    }
}
