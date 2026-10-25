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
    public Task<LiquidOrderApproval> Handle(GetOrderApprovalTokensCommand request, CancellationToken cancellationToken)
    {
        var approval = request.Approval;
        var url = (request.Host?.Url ?? request.Store.Url).Trim('/');

        var liquidOrderApproval = new LiquidOrderApproval {
            BuyerName = request.Buyer.GetFullName(),
            BuyerEmail = request.Buyer.Email,
            Status = approval.Status.ToString(),
            Total = approval.Summary?.Total,
            ItemsHtml = ItemsHtml(approval.Items),
            DecisionComment = Encode(approval.DecisionComment),
            Url = $"{url}/account/orderapproval?id={WebUtility.UrlEncode(approval.Id)}",
            CartUrl = $"{url}/cart"
        };
        return Task.FromResult(liquidOrderApproval);
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    /// <summary>
    ///     Every value in the table was typed or formatted outside of the template, so it is encoded here
    /// </summary>
    private static string ItemsHtml(IEnumerable<OrderApprovalItem> items)
    {
        var sb = new StringBuilder();
        sb.Append("<table style='width: 100%; border-collapse: collapse; font-size: 14px;' border='0'>");
        sb.Append("<tr style='background-color: #f4f4f5;'>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: left;'>Name</th>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: right;'>Price</th>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: center;'>Quantity</th>");
        sb.Append("<th style='padding: 0.6em 0.4em; text-align: right;'>Total</th></tr>");

        foreach (var item in items ?? [])
        {
            sb.Append("<tr style='border-bottom: 1px solid #e4e4e7;'>");
            sb.Append("<td style='padding: 0.6em 0.4em; text-align: left;'>").Append(Encode(item.ProductName));
            if (!string.IsNullOrEmpty(item.AttributeDescription))
                sb.Append("<br />").Append(Encode(item.AttributeDescription));
            if (!string.IsNullOrEmpty(item.Sku))
                sb.Append("<br />Sku: ").Append(Encode(item.Sku));
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
