using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> AccountTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "Customer.WelcomeMessage",
                Subject = "Welcome to {{Store.Name}}",
                Body =
                    "We welcome you to <a href=\"{{Store.URL}}\"> {{Store.Name}}</a>.<br />\r\n<br />\r\nYou can now take part in the various services we have to offer you. Some of these services include:<br />\r\n<br />\r\nPermanent Cart - Any products added to your online cart remain there until you remove them, or check them out.<br />\r\nAddress Book - We can now deliver your products to another address other than yours! This is perfect to send birthday gifts direct to the birthday-person themselves.<br />\r\nOrder History - View your history of purchases that you have made with us.<br />\r\nProducts Reviews - Share your opinions on products with our other customers.<br />\r\n<br />\r\nFor help with any of our online services, please email the store-owner: <a href=\"mailto:{{Store.Email}}\">{{Store.Email}}</a>.<br />\r\n<br />\r\nNote: This email address was provided on our registration page. If you own the email and did not register on our site, please send an email to <a href=\"mailto:{{Store.Email}}\">{{Store.Email}}</a>.",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.EmailValidationMessage",
                Subject = "{{Store.Name}}. Email validation",
                Body =
                    "<a href=\"{{Store.URL}}\">{{Store.Name}}</a>  <br />\r\n  <br />\r\n  To activate your account <a href=\"{{Customer.AccountActivationURL}}\">click here</a>.     <br />\r\n  <br />\r\n  {{Store.Name}}",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.EmailTokenValidationMessage",
                Subject = "{{Store.Name}} - Email Verification Code",
                Body =
                    "Hello {{Customer.FullName}}, <br /><br />\r\n Enter this 6 digit code on the sign in page to confirm your identity:<br /><br /> \r\n <b>{{Customer.Token}}</b><br /><br />\r\n Yours securely, <br /> \r\n Team",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.PasswordRecovery",
                Subject = "{{Store.Name}}. Password recovery",
                Body =
                    "<a href=\"{{Store.URL}}\">{{Store.Name}}</a>  <br />\r\n  <br />\r\n  To change your password <a href=\"{{Customer.PasswordRecoveryURL}}\">click here</a>.     <br />\r\n  <br />\r\n  {{Store.Name}}",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewPM",
                Subject = "{{Store.Name}}. You have received a new private message",
                Body =
                    "<p><a href=\"{{Store.URL}}\">{{Store.Name}}</a> <br />\r\n<br />\r\nYou have received a new private message.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewCustomerNote",
                Subject = "New customer note has been added",
                Body =
                    "<p><br />\r\nHello {{Customer.FullName}}, <br />\r\nNew customer note has been added to your account:<br />\r\n\"{{Customer.NewTitleText}}\".<br />\r\n</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewsLetterSubscription.ActivationMessage",
                Subject = "{{Store.Name}}. Subscription activation message.",
                Body =
                    "<p><a href=\"{{NewsLetterSubscription.ActivationUrl}}\">Click here to confirm your subscription to our list.</a></p><p>If you received this email by mistake, simply delete it.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewsLetterSubscription.DeactivationMessage",
                Subject = "{{Store.Name}}. Subscription deactivation message.",
                Body =
                    "<p><a href=\"{{NewsLetterSubscription.DeactivationUrl}}\">Click here to unsubscribe from our newsletter.</a></p><p>If you received this email by mistake, simply delete it.</p>",
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
