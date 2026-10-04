using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> ServiceTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "Service.EmailAFriend",
                Subject = "{{Store.Name}} — A product {{EmailAFriend.Email}} wanted to share",
                Body = Layout(
                    "A friend thought you'd like this product.",
                    "A product recommendation",
                    """
                    <p>{{EmailAFriend.Email}} was browsing {{Store.Name}} and wanted to share this product with you.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.6em 0.4em;'><strong>{{Product.Name}}</strong><br />{{Product.ShortDescription}}</td></tr>
                    </table>
                    {% if EmailAFriend.PersonalMessage != null and EmailAFriend.PersonalMessage != '' %}
                    <p>{{EmailAFriend.PersonalMessage}}</p>
                    {% endif %}
                    """ + Button("{{Product.ProductURLForCustomer}}", "View product")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Service.AskQuestion",
                Subject = "{{Store.Name}} — Question about {{Product.Name}}",
                Body = Layout(
                    "A customer has a question about a product.",
                    "New product question",
                    """
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.4em;'><strong>From:</strong></td><td style='padding: 0.4em;'>{{AskQuestion.FullName}}</td></tr>
                    <tr><td style='padding: 0.4em;'><strong>Email:</strong></td><td style='padding: 0.4em;'>{{AskQuestion.Email}}</td></tr>
                    <tr><td style='padding: 0.4em;'><strong>Phone:</strong></td><td style='padding: 0.4em;'>{{AskQuestion.Phone}}</td></tr>
                    <tr><td style='padding: 0.4em;'><strong>Product:</strong></td><td style='padding: 0.4em;'>{{Product.Name}}</td></tr>
                    </table>
                    <p>{{Product.ShortDescription}}</p>
                    <p>{{AskQuestion.Message}}</p>
                    """ + Button("{{Product.ProductURLForCustomer}}", "View product")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Service.ContactUs",
                Subject = "{{Store.Name}} — New contact us message",
                Body = Layout(
                    "A visitor sent a message through the contact form.",
                    "New contact form message",
                    """
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.4em;'><strong>From:</strong></td><td style='padding: 0.4em;'>{{ContactUs.SenderName}}</td></tr>
                    <tr><td style='padding: 0.4em;'><strong>Email:</strong></td><td style='padding: 0.4em;'>{{ContactUs.SenderEmail}}</td></tr>
                    </table>
                    <p>{{ContactUs.Body}}</p>
                    {% if ContactUs.AttributeDescription != null and ContactUs.AttributeDescription != '' %}
                    <p>{{ContactUs.AttributeDescription}}</p>
                    {% endif %}
                    """),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Service.ContactVendor",
                Subject = "{{Store.Name}} — New message from a customer",
                Body = Layout(
                    "A customer sent you a message through the store.",
                    "New customer message",
                    """
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.4em;'><strong>From:</strong></td><td style='padding: 0.4em;'>{{ContactUs.SenderName}}</td></tr>
                    <tr><td style='padding: 0.4em;'><strong>Email:</strong></td><td style='padding: 0.4em;'>{{ContactUs.SenderEmail}}</td></tr>
                    </table>
                    <p>{{ContactUs.Body}}</p>
                    """),
                IsActive = true,
                EmailAccountId = ea
            },

            new() {
                Name = "Wishlist.EmailAFriend",
                Subject = "{{Store.Name}} — {{EmailAFriend.Email}} shared a wishlist with you",
                Body = Layout(
                    "A friend shared their wishlist with you.",
                    "A wishlist shared with you",
                    """
                    <p>{{EmailAFriend.Email}} was shopping on {{Store.Name}} and wanted to share their wishlist with you.</p>
                    {% if EmailAFriend.PersonalMessage != null and EmailAFriend.PersonalMessage != '' %}
                    <p>{{EmailAFriend.PersonalMessage}}</p>
                    {% endif %}
                    """ + Button("{{Customer.WishlistURLForCustomer}}", "View wishlist")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Blog.BlogComment",
                Subject = "{{Store.Name}} — New comment on {{BlogComment.BlogPostTitle}}",
                Body = Layout(
                    "A new blog comment is waiting for review.",
                    "New blog comment",
                    """
                    <p>A new comment has been posted on the blog post: <strong>{{BlogComment.BlogPostTitle}}</strong>.</p>
                    """ + Button("{{BlogComment.BlogPostURL}}", "View post")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "News.NewsComment",
                Subject = "{{Store.Name}} — New comment on {{NewsComment.NewsTitle}}",
                Body = Layout(
                    "A new news comment is waiting for review.",
                    "New news comment",
                    """
                    <p>A new comment has been posted on the news article: <strong>{{NewsComment.NewsTitle}}</strong>.</p>
                    """ + Button("{{NewsComment.NewsURL}}", "View article")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Knowledgebase.ArticleComment",
                Subject = "{{Store.Name}} — New comment on {{Knowledgebase.ArticleCommentTitle}}",
                Body = Layout(
                    "A new article comment is waiting for review.",
                    "New article comment",
                    """
                    <p>A new comment has been posted on the article: <strong>{{Knowledgebase.ArticleCommentTitle}}</strong>.</p>
                    """ + Button("{{Knowledgebase.ArticleCommentUrl}}", "View article")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Product.ProductReview",
                Subject = "{{Store.Name}} — New review for {{ProductReview.ProductName}}",
                Body = Layout(
                    "A customer left a review for one of your products.",
                    "New product review",
                    """
                    <p>A new product review has been submitted for: <strong>{{ProductReview.ProductName}}</strong>.</p>
                    """),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Vendor.VendorReview",
                Subject = "{{Store.Name}} — New vendor review received",
                Body = Layout(
                    "A customer left a review for a vendor.",
                    "New vendor review",
                    """
                    <p>A new vendor review has been submitted for: <strong>{{VendorReview.VendorName}}</strong>.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.4em;'><strong>Title:</strong></td><td style='padding: 0.4em;'>{{VendorReview.Title}}</td></tr>
                    </table>
                    <p>{{VendorReview.ReviewText}}</p>
                    """),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "GiftVoucher.Notification",
                Subject = "{{Store.Name}} — {{GiftVoucher.SenderName}} sent you a gift voucher",
                Body = Layout(
                    "You have received a gift voucher.",
                    "You've received a gift voucher",
                    """
                    <p>Dear {{GiftVoucher.RecipientName}},</p>
                    <p>{{GiftVoucher.SenderName}} ({{GiftVoucher.SenderEmail}}) has sent you a gift voucher worth {{GiftVoucher.Amount}} for {{Store.Name}}.</p>
                    <table role='presentation' style='width: 100%; border-collapse: collapse; font-size: 14px;'>
                    <tr><td style='padding: 0.4em;'><strong>Voucher code:</strong></td><td style='padding: 0.4em;'>{{GiftVoucher.CouponCode}}</td></tr>
                    </table>
                    {% if GiftVoucher.Message != null and GiftVoucher.Message != '' %}
                    <p>{{GiftVoucher.Message}}</p>
                    {% endif %}
                    """ + Button("{{Store.URL}}", "Start shopping")),
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
