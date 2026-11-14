using Grand.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Adds the e-mail telling the store a customer wrote on a merchandise return. The installer creates the same for
///     new installations (it is not referenced from here, so the body is repeated below - a test keeps them equal).
///     Idempotent: a template that exists (the operator may have created or edited one) is never touched.
/// </summary>
public class MigrationMerchandiseReturnNoteStoreOwnerTemplate : IMigration
{
    private const string TemplateName = "NewMerchandiseReturnNote.StoreOwnerNotification";

    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("43bb8e56-71f5-4c8f-85a6-95ea3d25049a");
    public string Name => "Merchandise return customer message e-mail to the store 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationMerchandiseReturnNoteStoreOwnerTemplate>>();

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
            logService.LogError(ex, "UpgradeProcess - MerchandiseReturnNoteStoreOwnerTemplate (2.5)");
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
            Subject = "{{Store.Name}} — New message on return #{{MerchandiseReturn.ReturnNumber}}",
            Body = MigrationOrderApproval.Layout(
                "A customer wrote about a merchandise return.",
                "New message on a return",
                @"<p>The customer wrote about merchandise return #{{MerchandiseReturn.ReturnNumber}}.</p>
                {% if MerchandiseReturn.NewNoteText != null and MerchandiseReturn.NewNoteText != '' %}
                <p style='padding: 12px 16px; background-color: #f4f4f5; border-radius: 6px;'>{{MerchandiseReturn.NewNoteText}}</p>
                {% endif %}
                <p>Answer with a note shown to the customer on the return in the panel.</p>"),
            IsActive = true,
            EmailAccountId = emailAccountId
        };
    }
}
