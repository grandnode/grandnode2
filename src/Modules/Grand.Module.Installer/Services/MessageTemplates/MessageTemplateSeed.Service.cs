using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> ServiceTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "Service.EmailAFriend",
                Subject = "{{Store.Name}}. Referred Item",
                Body =
                    "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\n{{EmailAFriend.Email}} was shopping on {{Store.Name}} and wanted to share the following item with you. <br />\r\n<br />\r\n<b><a target=\"_blank\" href=\"{{Product.ProductURLForCustomer}}\">{{Product.Name}}</a></b> <br />\r\n{{Product.ShortDescription}} <br />\r\n<br />\r\nFor more info click <a target=\"_blank\" href=\"{{Product.ProductURLForCustomer}}\">here</a> <br />\r\n<br />\r\n<br />\r\n{{EmailAFriend.PersonalMessage}}<br />\r\n<br />\r\n{{Store.Name}}</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Service.AskQuestion",
                Subject = "{{Store.Name}}. Question about a product",
                Body =
                    "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\n{{AskQuestion.Email}} wanted to ask question about a product {{Product.Name}}. <br />\r\n<br />\r\n<b><a target=\"_blank\" href=\"{{Product.ProductURLForCustomer}}\">{{Product.Name}}</a></b> <br />\r\n{{Product.ShortDescription}} <br />\r\n{{AskQuestion.Message}}<br />\r\n {{AskQuestion.Email}} <br />\r\n {{AskQuestion.FullName}} <br />\r\n {{AskQuestion.Phone}} <br />\r\n{{Store.Name}}</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Service.ContactUs",
                Subject = "{{Store.Name}}. Contact us",
                Body =
                    "<p>From {{ContactUs.SenderName}} - {{ContactUs.SenderEmail}}<br /><br />{{ContactUs.Body}}<br />{{ContactUs.AttributeDescription}}</p><br />",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Service.ContactVendor",
                Subject = "{{Store.Name}}. Contact us",
                Body =
                    "<p>From {{ContactUs.SenderName}} - {{ContactUs.SenderEmail}}<br /><br />{{ContactUs.Body}}</p><br />",
                IsActive = true,
                EmailAccountId = ea
            },

            new() {
                Name = "Wishlist.EmailAFriend",
                Subject = "{{Store.Name}}. Wishlist",
                Body =
                    "<p><a href=\"{{Store.URL}}\"> {{Store.Name}}</a> <br />\r\n<br />\r\n{{EmailAFriend.Email}} was shopping on {{Store.Name}} and wanted to share a wishlist with you <br />\r\n<br />\r\n<br />\r\nFor more info click <a target=\"_blank\" href=\"{{Customer.WishlistURLForCustomer}}\">here</a> <br />\r\n<br />\r\n<br />\r\n{{EmailAFriend.PersonalMessage}}<br />\r\n<br />\r\n{{Store.Name}}</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Blog.BlogComment",
                Subject = "{{Store.Name}}. New blog comment.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new blog comment has been created for blog post \"{{BlogComment.BlogPostTitle}}\".</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "News.NewsComment",
                Subject = "{{Store.Name}}. New news comment.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new news comment has been created for news \"{{NewsComment.NewsTitle}}\".</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Knowledgebase.ArticleComment",
                Subject = "{{Store.Name}}. New article comment.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new article comment has been created for article \"{{Knowledgebase.ArticleCommentTitle}}\".</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Product.ProductReview",
                Subject = "{{Store.Name}}. New product review.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new product review has been written for product \"{{ProductReview.ProductName}}\".</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Vendor.VendorReview",
                Subject = "{{Store.Name}}. New vendor review.",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nA new vendor review has been written.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "GiftVoucher.Notification",
                Subject = "{{GiftVoucher.SenderName}} has sent you a gift voucher for {{Store.Name}}",
                Body =
                    "<p>You have received a gift voucher for {{Store.Name}}</p><p>Dear {{GiftVoucher.RecipientName}}, <br />\r\n<br />\r\n{{GiftVoucher.SenderName}} ({{GiftVoucher.SenderEmail}}) has sent you a {{GiftVoucher.Amount}} gift cart for <a href=\"{{Store.URL}}\"> {{Store.Name}}</a></p><p>You gift voucher code is {{GiftVoucher.CouponCode}}</p><p>{{GiftVoucher.Message}}</p>",
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
