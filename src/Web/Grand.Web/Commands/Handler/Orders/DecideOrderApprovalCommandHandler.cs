using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;

namespace Grand.Web.Commands.Handler.Orders;

public class DecideOrderApprovalCommandHandler : IRequestHandler<DecideOrderApprovalCommand, bool>
{
    private const int MaxCommentLength = 1000;

    private readonly ICustomerService _customerService;
    private readonly ICustomerMessageProviderService _messageProviderService;
    private readonly IOrderApprovalService _orderApprovalService;

    public DecideOrderApprovalCommandHandler(IOrderApprovalService orderApprovalService,
        ICustomerService customerService, ICustomerMessageProviderService messageProviderService)
    {
        _orderApprovalService = orderApprovalService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
    }

    public async Task<bool> Handle(DecideOrderApprovalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Approver);
        ArgumentNullException.ThrowIfNull(request.Store);

        // every refusal gives the same answer: a foreign, missing or finished request and a non-approver
        // cannot be told apart
        if (!OrderApprovalRules.CanApprove(request.Approver))
            return false;

        var approval = await _orderApprovalService.GetById(request.ApprovalId,
            OrderApprovalRules.CompanyId(request.Approver));
        if (approval == null || approval.StoreId != request.Store.Id || approval.Status != OrderApprovalStatus.Pending)
            return false;

        approval.Status = request.Approve ? OrderApprovalStatus.Approved : OrderApprovalStatus.Rejected;
        approval.DecidedOnUtc = DateTime.UtcNow;
        approval.DecidedByCustomerId = request.Approver.Id;
        approval.DecisionComment = NormalizeComment(request.Comment);
        await _orderApprovalService.Update(approval);

        var buyer = await _customerService.GetCustomerById(approval.CustomerId);
        if (buyer != null && !string.IsNullOrWhiteSpace(buyer.Email))
            await _messageProviderService.SendOrderApprovalDecidedMessage(approval, buyer, request.Store,
                request.Language?.Id, request.Host);

        return true;
    }

    private static string NormalizeComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;

        comment = comment.Trim();
        return comment.Length > MaxCommentLength ? comment[..MaxCommentLength] : comment;
    }
}
