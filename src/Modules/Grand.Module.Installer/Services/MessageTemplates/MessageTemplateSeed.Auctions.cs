using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> AuctionTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "AuctionEnded.CustomerNotificationWin",
                Subject = "{{Store.Name}} — You won the auction for {{Auctions.ProductName}}",
                Body = Layout(
                    "You won the auction for {{Auctions.ProductName}}.",
                    "You won the auction!",
                    @"<p>Hello {{Customer.FullName}},</p>
                <p>Congratulations — you won the auction for <a href='{{Store.URL}}{{Auctions.ProductSeName}}'>{{Auctions.ProductName}}</a>. The auction ended at {{Auctions.EndTime}} with your winning bid of {{Auctions.Price}}.</p>
                <p>Please complete your purchase to secure this item.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Product</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.ProductName}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Winning bid</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.Price}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Auction ended</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.EndTime}}</strong></td></tr>
                </table>" +
                    Button("{{Store.URL}}cart", "Go to cart")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionEnded.CustomerNotificationLost",
                Subject = "{{Store.Name}} — Auction ended for {{Auctions.ProductName}}",
                Body = Layout(
                    "The auction for {{Auctions.ProductName}} has ended.",
                    "You didn't win this time",
                    @"<p>Hello {{Customer.FullName}},</p>
                <p>The auction for {{Auctions.ProductName}} ended at {{Auctions.EndTime}} and unfortunately your bid wasn't the highest.</p>
                <p>Don't worry — new auctions are added all the time, so keep an eye out for the next one.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Product</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.ProductName}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Winning bid</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.Price}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Auction ended</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.EndTime}}</strong></td></tr>
                </table>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionEnded.CustomerNotificationBin",
                Subject = "{{Store.Name}} — Auction closed for {{Product.Name}}",
                Body = Layout(
                    "{{Product.Name}} was purchased with Buy It Now.",
                    "This item is no longer available",
                    @"<p>Hello {{Customer.FullName}},</p>
                <p>The auction for {{Product.Name}} has ended — another customer used the Buy It Now option and purchased it for {{Product.Price}}.</p>
                <p>We're sorry you missed out this time. New auctions are added regularly, so check back soon.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Product</td><td style='padding: 4px 0; text-align: right;'><strong>{{Product.Name}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Buy It Now price</td><td style='padding: 4px 0; text-align: right;'><strong>{{Product.Price}}</strong></td></tr>
                </table>" +
                    Button("{{Store.URL}}", "Continue shopping")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionEnded.StoreOwnerNotification",
                Subject = "{{Store.Name}} — {{Customer.FullName}} won the auction for {{Auctions.ProductName}}",
                Body = Layout(
                    "{{Customer.FullName}} won the auction for {{Auctions.ProductName}}.",
                    "Auction won",
                    @"<p>{{Customer.FullName}} won the auction for <a href='{{Store.URL}}{{Auctions.ProductSeName}}'>{{Auctions.ProductName}}</a> at {{Auctions.EndTime}}.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Customer</td><td style='padding: 4px 0; text-align: right;'><strong>{{Customer.FullName}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Product</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.ProductName}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Winning bid</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.Price}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Auction ended</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.EndTime}}</strong></td></tr>
                </table>" +
                    Button("{{Store.URL}}{{Auctions.ProductSeName}}", "View product")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionExpired.StoreOwnerNotification",
                Subject = "{{Store.Name}} — Auction for {{Product.Name}} expired without bids",
                Body = Layout(
                    "The auction for {{Product.Name}} closed without bids.",
                    "Auction expired without bids",
                    @"<p>The auction for {{Product.Name}} has expired without receiving any bids.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Product</td><td style='padding: 4px 0; text-align: right;'><strong>{{Product.Name}}</strong></td></tr>
                </table>"),
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "BidUp.CustomerNotification",
                Subject = "{{Store.Name}} — You've been outbid on {{Auctions.ProductName}}",
                Body = Layout(
                    "Your bid on {{Auctions.ProductName}} was outbid.",
                    "You've been outbid",
                    @"<p>Hi {{Customer.FullName}},</p>
                <p>Someone placed a higher bid on <a href='{{Store.URL}}{{Auctions.ProductSeName}}'>{{Auctions.ProductName}}</a>. Your bid was {{Auctions.Price}}.</p>
                <p>The auction closes at {{Auctions.EndTime}} — raise your bid now if you don't want to miss out.</p>
                <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px; margin: 16px 0;'>
                <tr><td style='padding: 4px 0; color: #71717a;'>Product</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.ProductName}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Your last bid</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.Price}}</strong></td></tr>
                <tr><td style='padding: 4px 0; color: #71717a;'>Auction ends</td><td style='padding: 4px 0; text-align: right;'><strong>{{Auctions.EndTime}}</strong></td></tr>
                </table>" +
                    Button("{{Store.URL}}{{Auctions.ProductSeName}}", "Raise your bid")),
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
