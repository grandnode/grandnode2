using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class ApplyOrderApprovalDecisionCommandHandler : IRequestHandler<ApplyOrderApprovalDecisionCommand, bool>
{
    private const int MaxCommentLength = 1000;

    private readonly ICustomerService _customerService;
    private readonly ILogger<ApplyOrderApprovalDecisionCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly ICustomerMessageProviderService _messageProviderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderService _orderService;
    private readonly IStoreService _storeService;

    public ApplyOrderApprovalDecisionCommandHandler(IOrderApprovalService orderApprovalService,
        ICustomerService customerService, ICustomerMessageProviderService messageProviderService,
        IOrderService orderService, IStoreService storeService, IMediator mediator,
        ILogger<ApplyOrderApprovalDecisionCommandHandler> logger)
    {
        _orderApprovalService = orderApprovalService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _orderService = orderService;
        _storeService = storeService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(ApplyOrderApprovalDecisionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Approval);
        ArgumentNullException.ThrowIfNull(request.DecidedBy);

        var approval = request.Approval;
        if (approval.Status != OrderApprovalStatus.Pending)
            return false;

        // nobody decides their own order, also not a buyer made an approver after placing it
        if (approval.CustomerId == request.DecidedBy.Id)
            return false;

        var order = await _orderService.GetOrderById(approval.OrderId);
        if (order == null || order.OrderStatusId != (int)OrderStatusSystem.AwaitingApproval)
            return false;

        var buyer = await _customerService.GetCustomerById(approval.CustomerId);

        // the request closes first: of two decisions at once only one goes on, and a failed save leaves the order waiting
        if (!await _orderApprovalService.Close(approval,
                request.Approve ? OrderApprovalStatus.Approved : OrderApprovalStatus.Rejected, request.DecidedBy.Id,
                NormalizeComment(request.Comment)))
            return false;

        try
        {
            if (request.Approve)
                // the order was placed in the awaiting status; nobody is told about the status change itself
                await _mediator.Send(new SetOrderStatusCommand {
                    Order = order, Os = OrderStatusSystem.Pending, NotifyCustomer = false, NotifyStoreOwner = false
                }, cancellationToken);
            else
                // the customer is not told about the cancellation: the decision e-mail below says it
                await _mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = false },
                    cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Order approval {Id}: the order could not be moved, the request goes back to pending",
                approval.Id);
            await Guard(approval.Id, "reopening the request", () => _orderApprovalService.Reopen(approval));
            return false;
        }

        // from here the order is an ordinary one: a later failure must not undo or abort the decision
        await Guard(order.Id, "order note", () => _orderService.InsertOrderNote(new OrderNote {
            Note = $"Order {(request.Approve ? "approved" : "rejected")} by {request.DecidedBy.Email ?? request.DecidedBy.Id}",
            DisplayToCustomer = false,
            OrderId = order.Id
        }));

        if (request.Approve)
            await ContinueApproved(order, buyer, request, cancellationToken);

        await Guard(approval.Id, "decision e-mail", async () =>
        {
            if (buyer == null)
                return;

            // the buyer reads it: in the language the order was placed in, not the approver's
            var store = await _storeService.GetStoreById(approval.StoreId);
            var languageId = string.IsNullOrEmpty(order.CustomerLanguageId)
                ? request.Language?.Id
                : order.CustomerLanguageId;

            // a guest has no e-mail of their own: the address they gave is on the order
            if (!string.IsNullOrWhiteSpace(buyer.Email))
                await _messageProviderService.SendOrderApprovalDecidedMessage(approval, buyer, store, languageId,
                    request.Host);
            else if (!string.IsNullOrWhiteSpace(order.BillingAddress?.Email))
                await _messageProviderService.SendOrderApprovalDecidedMessage(approval, buyer, store, languageId,
                    request.Host, order.BillingAddress.Email);
        });

        return true;
    }

    private async Task ContinueApproved(Order order, Customer buyer, ApplyOrderApprovalDecisionCommand request,
        CancellationToken cancellationToken)
    {
        await Guard(order.Id, "order status check",
            () => _mediator.Send(new CheckOrderStatusCommand { Order = order }, cancellationToken));

        // the customer got the "order placed" e-mail at placement; the store owner and the vendors hear now
        await Guard(order.Id, "store owner and vendor e-mails", () => _mediator.Send(new OrderNotificationCommand {
            Order = order, Customer = buyer, Host = request.Host, NotifyCustomer = false,
            NotifyStoreOwnerAndVendors = true, AddOrderPlacedNote = false
        }, cancellationToken));

        // the "paid" processing was held back at placement
        if (order.PaymentStatusId == PaymentStatus.Paid)
            await Guard(order.Id, "paid processing",
                () => _mediator.Send(new ProcessOrderPaidCommand { Order = order }, cancellationToken));
    }

    /// <summary>
    ///     A failed follow-up step must not abort a decision that is already saved
    /// </summary>
    private async Task Guard(string id, string step, Func<Task> run)
    {
        try
        {
            await run();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Order approval {Id}: {Step} failed after the decision was saved", id, step);
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
