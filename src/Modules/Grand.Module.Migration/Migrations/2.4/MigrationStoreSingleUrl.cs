using Grand.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Collapses the store address onto a single <see cref="Store.Url" />. Until 2.4 a store carried
///     <c>Url</c>, <c>SecureUrl</c> and <c>SslEnabled</c>, and every consumer resolved the effective
///     address as <c>SslEnabled ? SecureUrl : Url</c>. Both extra fields are gone - <c>Url</c> now holds
///     the full address including the scheme, and forcing HTTPS belongs to the host (UseHttpsRedirection,
///     UseHsts, the reverse proxy), not to the store record.
///     The effective address of an existing installation is already stored on the primary
///     <see cref="DomainHost" />: the installer, the admin store editor and the 1.1 domains migration all
///     wrote exactly that resolved value there. This copies it back onto <c>Url</c>, so a store that ran on
///     HTTPS keeps generating HTTPS links in emails, sitemap.xml and robots.txt.
///     The now unmapped <c>SslEnabled</c> and <c>SecureUrl</c> elements are left in the documents - the
///     global IgnoreExtraElementsConvention skips them on read, and they disappear the next time the store
///     is saved.
/// </summary>
public class MigrationStoreSingleUrl : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("9E3C7A21-08B4-4F6D-B5A2-7D14C6E39F80");
    public string Name => "Collapse store Url/SecureUrl/SslEnabled onto a single Url 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Store>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationStoreSingleUrl>>();

        try
        {
            foreach (var store in repository.Table.ToList())
            {
                var effectiveUrl = store.Domains?.FirstOrDefault(x => x.Primary)?.Url;
                if (string.IsNullOrWhiteSpace(effectiveUrl) ||
                    !Uri.TryCreate(effectiveUrl.Trim(), UriKind.Absolute, out _))
                {
                    //nothing reliable to fall back to - the store keeps whatever Url it had
                    logService.LogWarning(
                        "Store {StoreName} has no usable primary domain - its Url was left as {StoreUrl}. Verify it in the admin area, it is used to build links in emails and sitemap.xml",
                        store.Name, store.Url);
                    continue;
                }

                //the 1.1 domains migration wrote the primary domain without a trailing slash, while the store
                //url is concatenated with relative paths (robots.txt, sitemap.xml)
                effectiveUrl = effectiveUrl.Trim();
                if (!effectiveUrl.EndsWith("/"))
                    effectiveUrl += "/";

                if (effectiveUrl == store.Url)
                    continue;

                store.Url = effectiveUrl;
                repository.Update(store);
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MigrationStoreSingleUrl (2.4)");
        }

        return true;
    }
}
