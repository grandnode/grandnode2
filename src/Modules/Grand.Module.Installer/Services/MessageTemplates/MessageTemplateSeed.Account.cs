using Grand.Domain.Messages;

namespace Grand.Module.Installer.Services.MessageTemplates;

public static partial class MessageTemplateSeed
{
    private static IEnumerable<MessageTemplate> AccountTemplates(string ea)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "Customer.WelcomeMessage",
                Subject = "{{Store.Name}} — Welcome to your new account",
                Body = Layout(
                    "Welcome to {{Store.Name}} — here's what your account can do",
                    "Welcome to {{Store.Name}}",
                    @"
                <p>Thanks for creating an account with <strong>{{Store.Name}}</strong>. You can now enjoy a faster, more personal shopping experience.</p>
                <p>Here is what your account gives you:</p>
                <p>
                <strong>Cart that remembers you</strong> — anything you add stays in your cart until you check out.<br />
                <strong>Address book</strong> — save more than one delivery address, handy for sending gifts.<br />
                <strong>Order history</strong> — see every purchase you have made with us.<br />
                <strong>Product reviews</strong> — share your opinion with other shoppers.
                </p>" +
                    Button("{{Store.URL}}", "Start shopping") + @"
                <p>Need a hand with anything? Email us at <a href='mailto:{{Store.Email}}'>{{Store.Email}}</a>.</p>
                <p>This address was used to register on our site. If that was not you, please let us know at <a href='mailto:{{Store.Email}}'>{{Store.Email}}</a>.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.EmailValidationMessage",
                Subject = "{{Store.Name}} — Confirm your email address",
                Body = Layout(
                    "Confirm your email to activate your account",
                    "Confirm your email address",
                    @"
                <p>Thanks for registering at <strong>{{Store.Name}}</strong>. Please confirm your email address to activate your account.</p>" +
                    Button("{{Customer.AccountActivationURL}}", "Activate my account") + @"
                <p>If you did not create an account with us, you can safely ignore this email.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.EmailTokenValidationMessage",
                Subject = "{{Store.Name}} — Your verification code",
                Body = Layout(
                    "Your sign-in verification code",
                    "Verify your identity",
                    @"
                <p>Hello {{Customer.FullName}},</p>
                <p>Enter this 6-digit code on the sign-in page to confirm it is you:</p>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='margin: 16px 0;'>
                <tr><td style='padding: 0.8em 1em; background-color: #f4f4f5; border-radius: 6px; text-align: center; font-size: 26px; font-weight: bold; letter-spacing: 6px; color: #18181b;'>{{Customer.Token}}</td></tr>
                </table>
                <p>If you did not request this code, you can ignore this email — your account is still secure.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.PasswordRecovery",
                Subject = "{{Store.Name}} — Reset your password",
                Body = Layout(
                    "Reset the password for your account",
                    "Reset your password",
                    @"
                <p>We received a request to reset the password for your account at <strong>{{Store.Name}}</strong>.</p>" +
                    Button("{{Customer.PasswordRecoveryURL}}", "Reset my password") + @"
                <p>If you did not request a password reset, you can safely ignore this email — your password will not be changed.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewPM",
                Subject = "{{Store.Name}} — You have a new private message",
                Body = Layout(
                    "You have received a new private message",
                    "New private message",
                    @"
                <p>You have received a new private message.</p>" +
                    Button("{{Store.URL}}", "Visit {{Store.Name}}")),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "Customer.NewCustomerNote",
                Subject = "{{Store.Name}} — A note has been added to your account",
                Body = Layout(
                    "A new note has been added to your account",
                    "New note on your account",
                    @"
                <p>Hello {{Customer.FullName}},</p>
                <p>{{Store.Name}} has added the following note to your account:</p>
                <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='margin: 16px 0;'>
                <tr><td style='padding: 0.8em 1em; background-color: #f4f4f5; border-radius: 6px;'>{{Customer.NewTitleText}}</td></tr>
                </table>
                <p>If you have any questions, just reply to this email.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewsLetterSubscription.ActivationMessage",
                Subject = "{{Store.Name}} — Confirm your newsletter subscription",
                Body = Layout(
                    "Confirm your newsletter subscription",
                    "Confirm your subscription",
                    @"
                <p>Thanks for subscribing to updates from <strong>{{Store.Name}}</strong>.</p>" +
                    Button("{{NewsLetterSubscription.ActivationUrl}}", "Confirm my subscription") + @"
                <p>If you did not request this, simply ignore this email — you will not be subscribed.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
            new() {
                Name = "NewsLetterSubscription.DeactivationMessage",
                Subject = "{{Store.Name}} — Confirm you want to unsubscribe",
                Body = Layout(
                    "Confirm you want to leave our newsletter",
                    "Unsubscribe from our newsletter",
                    @"
                <p>We received a request to remove this email address from the <strong>{{Store.Name}}</strong> newsletter.</p>" +
                    Button("{{NewsLetterSubscription.DeactivationUrl}}", "Unsubscribe me") + @"
                <p>If you did not request this, simply ignore this email — you will stay subscribed.</p>"),
                IsActive = true,
                EmailAccountId = ea
            },
        };
    }
}
