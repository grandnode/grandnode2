using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;

namespace Grand.Web.Features.Handlers.Orders;

public class GetOrderApprovalHandler : IRequestHandler<GetOrderApproval, OrderApprovalModel>
{
    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEnumTranslationService _enumTranslationService;
    private readonly IOrderApprovalService _orderApprovalService;

    public GetOrderApprovalHandler(IOrderApprovalService orderApprovalService, ICustomerService customerService,
        IDateTimeService dateTimeService, IEnumTranslationService enumTranslationService)
    {
        _orderApprovalService = orderApprovalService;
        _customerService = customerService;
        _dateTimeService = dateTimeService;
        _enumTranslationService = enumTranslationService;
    }

    public async Task<OrderApprovalModel> Handle(GetOrderApproval request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);

        if (string.IsNullOrEmpty(request.Id))
            return null;

        // a missing, foreign-company, other-store or somebody else's request all give the same answer: null
        var approval = await _orderApprovalService.GetById(request.Id, OrderApprovalRules.CompanyId(request.Customer));
        if (approval == null || approval.StoreId != request.Store.Id)
            return null;

        var isApprover = OrderApprovalRules.CanApprove(request.Customer);
        var isBuyer = approval.CustomerId == request.Customer.Id;
        if (!isApprover && !isBuyer)
            return null;

        var model = new OrderApprovalModel {
            Id = approval.Id,
            CreatedOn = _dateTimeService.ConvertToUserTime(approval.CreatedOnUtc, DateTimeKind.Utc),
            StatusId = (int)approval.Status,
            Status = _enumTranslationService.GetTranslationEnum(approval.Status),
            DecisionComment = approval.DecisionComment,
            CanDecide = isApprover && approval.Status == OrderApprovalStatus.Pending,
            CanWithdraw = isBuyer && approval.Status == OrderApprovalStatus.Pending
        };

        if (approval.DecidedOnUtc.HasValue)
        {
            model.DecidedOn = _dateTimeService.ConvertToUserTime(approval.DecidedOnUtc.Value, DateTimeKind.Utc);
            if (!string.IsNullOrEmpty(approval.DecidedByCustomerId))
                model.DecidedBy = OrderApprovalBuyerName.For(
                    await _customerService.GetCustomerById(approval.DecidedByCustomerId));
        }

        var buyer = isBuyer ? request.Customer : await _customerService.GetCustomerById(approval.CustomerId);
        model.Buyer = OrderApprovalBuyerName.For(buyer);

        foreach (var item in approval.Items)
            model.Items.Add(new OrderApprovalModel.OrderApprovalItemModel {
                ProductName = item.ProductName,
                Sku = item.Sku,
                Quantity = item.Quantity,
                AttributeDescription = item.AttributeDescription,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            });

        var summary = approval.Summary;
        if (summary != null)
            model.Summary = new OrderApprovalModel.OrderApprovalSummaryModel {
                SubTotal = summary.SubTotal,
                Discount = summary.Discount,
                ShippingMethod = summary.ShippingMethod,
                Shipping = summary.Shipping,
                Tax = summary.Tax,
                Total = summary.Total
            };

        return model;
    }
}
