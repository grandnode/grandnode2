using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;
using System.Net;
using System.Text;

namespace Grand.Business.Messages.Commands.Handlers.Messages;

public class GetOrderApprovalTokensCommandHandler : IRequestHandler<GetOrderApprovalTokensCommand, LiquidOrderApproval>
{
    private readonly IOrderService _orderService;
    private readonly ITranslationService _translationService;

    public GetOrderApprovalTokensCommandHandler(ITranslationService translationService, IOrderService orderService)
    {
        _translationService = translationService;
        _orderService = orderService;
    }

    public async Task<LiquidOrderApproval> Handle(GetOrderApprovalTokensCommand request, CancellationToken cancellationToken)
    {
        var approval = request.Approval;
        var url = (request.Host?.Url ?? request.Store.Url).Trim('/');
        //the order can be gone (deleted by an admin); the link then simply leads to a missing order
        var order = string.IsNullOrEmpty(approval.OrderId) ? null : await _orderService.GetOrderById(approval.OrderId);

        var liquidOrderApproval = new LiquidOrderApproval {
            BuyerName = request.Buyer.GetFullName(),
            BuyerEmail = request.Buyer.Email,
            Status = approval.Status.GetTranslationEnum(_translationService, request.Language.Id),
            Total = approval.Summary?.Total,
            ItemsHtml = ItemsHtml(approval.Items, request.Language.Id),
            DecisionComment = Encode(approval.DecisionComment),
            Url = $"{url}/account/orderapproval?id={WebUtility.UrlEncode(approval.Id)}",
            OrderNumber = order?.OrderNumber.ToString() ?? string.Empty,
            OrderUrl = $"{url}/orderdetails/{WebUtility.UrlEncode(approval.OrderId)}"
        };
        return liquidOrderApproval;
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private string Label(string key, string defaultValue, string languageId)
    {
        return Encode(_translationService.GetResource(key, languageId, defaultValue));
    }

    /// <summary>
    ///     Every value in the table was typed or formatted outside of the template, so it is encoded here -
    ///     the labels too, they come from a translator
    /// </summary>
    private string ItemsHtml(IEnumerable<OrderApprovalItem> items, string languageId)
    {
        var sb = new StringBuilder();
        sb.Append("<table style='width: 100%; border-collapse: collapse; font-size: 14px;' border='0'>");
        sb.Append("<tr style='background-color: #f4f4f5;'>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: left;'>")
            .Append(Label("OrderApproval.Email.Product", "Product", languageId)).Append("</th>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: right;'>")
            .Append(Label("OrderApproval.Email.Price", "Price", languageId)).Append("</th>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: center;'>")
            .Append(Label("OrderApproval.Email.Quantity", "Quantity", languageId)).Append("</th>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: right;'>")
            .Append(Label("OrderApproval.Email.Total", "Total", languageId)).Append("</th></tr>");

        foreach (var item in items ?? [])
        {
            sb.Append("<tr style='border-bottom: 1px solid #e4e4e7;'>");
            sb.Append("<td style='padding: 0.6em 0.4em; text-align: left;'>").Append(Encode(item.ProductName));
            if (!string.IsNullOrEmpty(item.AttributeDescription))
                sb.Append("<br />").Append(Encode(item.AttributeDescription));
            if (!string.IsNullOrEmpty(item.Sku))
                sb.Append("<br />").Append(Label("OrderApproval.Email.Sku", "SKU", languageId)).Append(": ")
                    .Append(Encode(item.Sku));
            sb.Append("</td>");
            sb.Append("<td style='padding: 0.6em 0.4em; text-align: right;'>").Append(Encode(item.UnitPrice))
                .Append("</td>");
            sb.Append("<td style='padding: 0.6em 0.4em; text-align: center;'>").Append(item.Quantity)
                .Append("</td>");
            sb.Append("<td style='padding: 0.6em 0.4em; text-align: right;'>").Append(Encode(item.LineTotal))
                .Append("</td></tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }
}
