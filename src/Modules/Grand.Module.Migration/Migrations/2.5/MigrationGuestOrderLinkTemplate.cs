using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Adds the e-mail with the link that opens an order placed without an account. The installer creates the same for new
///     installations (it is not referenced from here, so the body is repeated below - a test keeps them equal).
///     Idempotent: a template that exists (the operator may have created or edited one) is never touched.
/// </summary>
public class MigrationGuestOrderLinkTemplate : IMigration
{
    private const string TemplateName = "GuestOrderLink.CustomerNotification";

    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("6c587574-10ef-4a85-8c7e-84e071c86ff4");
    public string Name => "Guest order link e-mail 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationGuestOrderLinkTemplate>>();

        try
        {
            var repository = serviceProvider.GetRequiredService<IRepository<MessageTemplate>>();
            var existing = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.Name == TemplateName));
            if (existing != null)
                return true;

            await repository.InsertAsync(Template(await MigrationOrderApproval.GetEmailAccountId(serviceProvider)));

            //a lookup made before the upgrade may have cached "no such template"
            var cache = serviceProvider.GetService<ICacheBase>();
            if (cache != null)
                await cache.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - GuestOrderLinkTemplate (2.5)");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     The template, as the installer seeds it: same name, subject and tokens
    /// </summary>
    public static MessageTemplate Template(string emailAccountId)
    {
        return new MessageTemplate {
            Name = TemplateName,
            Subject = "{{Store.Name}} — Your order {{GuestOrderLink.OrderCode}}",
            Body = MigrationOrderApproval.Layout(
                "Your link to your order.",
                "Your order",
                @"<p>You asked to see order {{GuestOrderLink.OrderCode}}.</p>
                " + MigrationOrderApproval.Button("{{GuestOrderLink.Url}}", "View your order") + @"
                <p>The link is valid for 24 hours. If you did not ask for it, ignore this e-mail.</p>"),
            IsActive = true,
            EmailAccountId = emailAccountId
        };
    }
}
