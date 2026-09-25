using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> AuctionTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "AuctionEnded.CustomerNotificationWin",
                Subject = "{{Store.Name}}. Auction ended.",
                Body =
                    "<p>Hello, {{Customer.FullName}}!</p><p></p><p>At {{Auctions.EndTime}} you have won <a href=\"{{Store.URL}}{{Auctions.ProductSeName}}\">{{Auctions.ProductName}}</a> for {{Auctions.Price}}. Visit  <a href=\"{{Store.URL}}/cart\">cart</a> to finish checkout process. </p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionEnded.CustomerNotificationLost",
                Subject = "{{Store.Name}}. Auction ended.",
                Body =
                    "<p>Hello, {{Customer.FullName}}!</p><p></p><p>Unfortunately you did not win the bid {{Auctions.ProductName}}</p> <p>End price:  {{Auctions.Price}} </p> <p>End date auction {{Auctions.EndTime}} </p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionEnded.CustomerNotificationBin",
                Subject = "{{Store.Name}}. Auction ended.",
                Body =
                    "<p>Hello, {{Customer.FullName}}!</p><p></p><p>Unfortunately you did not win the bid {{Product.Name}}</p> <p>Product was bought by option Buy it now for price: {{Product.Price}} </p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionEnded.StoreOwnerNotification",
                Subject = "{{Store.Name}}. Auction ended.",
                Body =
                    "<p>At {{Auctions.EndTime}} {{Customer.FullName}} have won <a href=\"{{Store.URL}}{{Auctions.ProductSeName}}\">{{Auctions.ProductName}}</a> for {{Auctions.Price}}.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "AuctionExpired.StoreOwnerNotification",
                Subject = "Your auction to product {{Product.Name}}  has expired.",
                Body = "Hello, <br> Your auction to product {{Product.Name}} has expired without bid.",
                IsActive = false,
                EmailAccountId = ea
            },
            new() {
                Name = "BidUp.CustomerNotification",
                Subject = "{{Store.Name}}. Your offer has been outbid.",
                Body =
                    "<p>Hi {{Customer.FullName}}!</p><p>Your offer for product <a href=\"{{Store.URL}}{{Auctions.ProductSeName}}\">{{Auctions.ProductName}}</a> has been outbid. Your price was {{Auctions.Price}}.<br />\r\nRaise a price by raising one's offer. Auction will be ended on {{Auctions.EndTime}}</p>",
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
