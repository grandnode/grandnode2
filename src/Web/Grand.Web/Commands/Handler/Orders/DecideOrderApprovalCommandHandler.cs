using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Commands.Handler.Orders;

public class DecideOrderApprovalCommandHandler : IRequestHandler<DecideOrderApprovalCommand, bool>
{
    private const int MaxCommentLength = 1000;

    private readonly ICustomerService _customerService;
    private readonly ILogger<DecideOrderApprovalCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ICustomerMessageProviderService _messageProviderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderService _orderService;

    public DecideOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService,
        ICustomerService customerService, ICustomerMessageProviderService messageProviderService,
        IOrderService orderService, IMediator mediator, ILogger<DecideOrderApprovalCommandHandler> logger)
    {
        _orderApprovalService = orderApprovalService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _orderService = orderService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(DecideOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Approver);
        ArgumentNullException.ThrowIfNull(request.Store);

        // every refusal gives the same answer: a foreign, missing or finished request, an order that no longer
        // awaits the decision and a non-approver cannot be told apart
        if (!OrderApprovalRules.CanApprove(request.Approver))
            return false;

        var approval = await _orderApprovalService.GetById(request.ApprovalId,
            OrderApprovalRules.CompanyId(request.Approver));
        if (approval == null || approval.StoreId != request.Store.Id || approval.Status != OrderApprovalStatus.Pending)
            return false;

        var order = await _orderService.GetOrderById(approval.OrderId);
        if (order == null || order.OrderStatusId != (int)OrderStatusSystem.AwaitingApproval)
            return false;

        approval.Status = request.Approve ? OrderApprovalStatus.Approved : OrderApprovalStatus.Rejected;
        approval.DecidedOnUtc = DateTime.UtcNow;
        approval.DecidedByCustomerId = request.Approver.Id;
        approval.DecisionComment = NormalizeComment(request.Comment);
        await _orderApprovalService.Update(approval);

        var buyer = await _customerService.GetCustomerById(approval.CustomerId);

        if (request.Approve)
            await Approve(order, buyer, request, cancellationToken);
        else
            // the customer is not told about the cancellation: the decision e-mail below says it
            await _mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = false }, cancellationToken);

        await Notify(approval.Id, async () =>
        {
            if (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email))
                await _messageProviderService.SendOrderApprovalDecidedMessage(approval, buyer, request.Store,
                    request.Language?.Id, request.Host);
        });

        return true;
    }

    private async Task Approve(Order order, Customer buyer, DecideOrderApprovalCommand request,
        CancellationToken cancellationToken)
    {
        // the order was placed in the awaiting status; nobody is told about the status change itself
        await _mediator.Send(new SetOrderStatusCommand {
            Order = order, Os = OrderStatusSystem.Pending, NotifyCustomer = false, NotifyStoreOwner = false
        }, cancellationToken);
        await _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken);

        // the customer got the "order placed" e-mail at placement; the store owner and the vendors hear now
        await Notify(order.Id, () => _mediator.Send(new OrderNotificationCommand {
            Order = order, Customer = buyer, Host = request.Host, NotifyCustomer = false,
            NotifyStoreOwnerAndVendors = true
        }, cancellationToken));

        // the "paid" processing was held back at placement
        if (order.PaymentStatusId == PaymentStatus.Paid)
            await _mediator.Send(new ProcessOrderPaidCommand { Order = order }, cancellationToken);
    }

    /// <summary>
    ///     A failed e-mail must neither undo nor abort a decision that is already saved
    /// </summary>
    private async Task Notify(string id, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Order approval e-mail for {Id} could not be sent", id);
        }
    }

    private static string NormalizeComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;

        comment = comment.Trim();
        return comment.Length > MaxCommentLength ? comment[..MaxCommentLength] : comment;
    }
}
