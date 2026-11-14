using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Grand.Module.Migration.Migrations._2._5.MigrationOrderApproval;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Adds the customer's e-mails for a merchandise return accepted, received, rejected and completed; the generic
///     status changed e-mail stays for the other changes and for a store without these. The installer creates the
///     same for new installations (it is not referenced from here, so the bodies are repeated below - a test keeps
///     them equal). Idempotent: a template that exists (the operator may have created or edited one) is never touched.
/// </summary>
public class MigrationMerchandiseReturnStatusTemplates : IMigration
{
    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("eae5513a-a486-423a-b45e-7bbb0132f396");
    public string Name => "Merchandise return e-mails per status 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationMerchandiseReturnStatusTemplates>>();

        try
        {
            var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
            var emailAccountId = await GetEmailAccountId(serviceProvider);
            foreach (var template in Templates(emailAccountId))
            {
                var existing =
                    await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.Name == template.Name));
                if (existing == null)
                    await repository.InsertAsync(template);
            }

            //a lookup made before the upgrade may have cached "no such template"
            var cache = serviceProvider.GetService<ICacheBase>();
            if (cache != null)
                await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MerchandiseReturnStatusTemplates (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     The templates, as the installer seeds them: same names, subjects and tokens
    /// </summary>
    public static IList<MessageTemplate> Templates(string emailAccountId)
    {
        return new List<MessageTemplate> {
            new() {
                Name = "MerchandiseReturnAccepted.CustomerNotification",
                Subject = "{{Store.Name}} — Return #{{MerchandiseReturn.ReturnNumber}} accepted",
                Body =
                    Layout(
                        "Your merchandise return was accepted.",
                        "Your return was accepted",
                        @"<p>Hello {{Customer.FullName}},</p>
                <p>We accepted your merchandise return #{{MerchandiseReturn.ReturnNumber}}. Here is how to send the items back.</p>
                {% if MerchandiseReturn.ReturnMethod != '' %}
                <p><strong>Return method</strong><br />{{MerchandiseReturn.ReturnMethod}}</p>
                {% endif %}
                {% if MerchandiseReturn.ReturnCode != '' %}
                <p><strong>Return code</strong><br />{{MerchandiseReturn.ReturnCode}}</p>
                {% endif %}
                {% if MerchandiseReturn.ReturnInstructions != '' %}
                <p>{{MerchandiseReturn.ReturnInstructions}}</p>
                {% endif %}
                {% if MerchandiseReturn.ReturnLabelUrl != '' %}
                " + Button("{{MerchandiseReturn.ReturnLabelUrl}}", "Your return label") + @"
                {% else %}
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return") + @"
                {% endif %}"),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "MerchandiseReturnReceived.CustomerNotification",
                Subject = "{{Store.Name}} — Return #{{MerchandiseReturn.ReturnNumber}} received",
                Body =
                    Layout(
                        "The items of your return arrived.",
                        "We received your return",
                        @"<p>Hello {{Customer.FullName}},</p>
                <p>We received the items of your merchandise return #{{MerchandiseReturn.ReturnNumber}} and are checking them. We will let you know once your return is settled.</p>
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return") + @""),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "MerchandiseReturnRejected.CustomerNotification",
                Subject = "{{Store.Name}} — Return #{{MerchandiseReturn.ReturnNumber}} rejected",
                Body =
                    Layout(
                        "Your merchandise return was not accepted.",
                        "Your return was not accepted",
                        @"<p>Hello {{Customer.FullName}},</p>
                <p>We could not accept your merchandise return #{{MerchandiseReturn.ReturnNumber}}.</p>
                {% if MerchandiseReturn.RejectionReason != '' %}
                <p><strong>Reason</strong><br />{{MerchandiseReturn.RejectionReason}}</p>
                {% endif %}
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return") + @"
                <p>If you have any questions about your return, just reply to this email.</p>"),
                IsActive = true,
                EmailAccountId = emailAccountId
            },
            new() {
                Name = "MerchandiseReturnCompleted.CustomerNotification",
                Subject = "{{Store.Name}} — Return #{{MerchandiseReturn.ReturnNumber}} completed",
                Body =
                    Layout(
                        "Your merchandise return is complete.",
                        "Your return is complete",
                        @"<p>Hello {{Customer.FullName}},</p>
                {% if MerchandiseReturn.RefundAmount != '' %}
                <p>We refunded <strong>{{MerchandiseReturn.RefundAmount}}</strong> for your merchandise return #{{MerchandiseReturn.ReturnNumber}}. It may take a few days to reach your account.</p>
                {% else %}
                <p>Your merchandise return #{{MerchandiseReturn.ReturnNumber}} is complete.</p>
                {% endif %}
                " + Button("{{Store.URL}}merchandisereturndetails/{{MerchandiseReturn.Id}}", "View return") + @""),
                IsActive = true,
                EmailAccountId = emailAccountId
            }
        };
    }
}
