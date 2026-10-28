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

        // nobody decides their own order, also not a buyer made an approver after placing it
        if (approval.CustomerId == request.Approver.Id)
            return false;

        var order = await _orderService.GetOrderById(approval.OrderId);
        if (order == null || order.OrderStatusId != (int)OrderStatusSystem.AwaitingApproval)
            return false;

        var buyer = await _customerService.GetCustomerById(approval.CustomerId);

        // the order moves first: when that fails the request stays Pending and can be decided again
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
            _logger.LogError(e, "Order approval {Id}: the order could not be moved, the request stays pending",
                approval.Id);
            return false;
        }

        approval.Status = request.Approve ? OrderApprovalStatus.Approved : OrderApprovalStatus.Rejected;
        approval.DecidedOnUtc = DateTime.UtcNow;
        approval.DecidedByCustomerId = request.Approver.Id;
        approval.DecisionComment = NormalizeComment(request.Comment);
        await _orderApprovalService.Update(approval);

        // from here the order is an ordinary one: a later failure must not undo or abort the decision
        if (request.Approve)
            await ContinueApproved(order, buyer, request, cancellationToken);

        await Guard(approval.Id, "decision e-mail", async () =>
        {
            if (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email))
                // the buyer reads it: in the language the order was placed in, not the approver's
                await _messageProviderService.SendOrderApprovalDecidedMessage(approval, buyer, request.Store,
                    string.IsNullOrEmpty(order.CustomerLanguageId) ? request.Language?.Id : order.CustomerLanguageId,
                    request.Host);
        });

        return true;
    }

    private async Task ContinueApproved(Order order, Customer buyer, DecideOrderApprovalCommand request,
        CancellationToken cancellationToken)
    {
        await Guard(order.Id, "order note", () => _orderService.InsertOrderNote(new OrderNote {
            Note = "Order approved", DisplayToCustomer = false, OrderId = order.Id
        }));

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
    ///     A failed follow-up step must neither undo nor abort a decision that is already saved
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
