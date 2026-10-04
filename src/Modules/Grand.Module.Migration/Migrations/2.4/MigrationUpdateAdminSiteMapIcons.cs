using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Rewrites the icon of every admin menu entry from Font Awesome 4 and simple-line-icons to
///     bootstrap-icons. The panels no longer load either font: Bootstrap 5.3 brought bootstrap-icons,
///     which is the font the storefront already ships, and the two old stylesheets are gone.
///     <see cref="AdminSiteMap.IconClass" /> is stored per installation, seeded once by the installer,
///     so without this an installation that was set up before the upgrade shows a menu with no icons at
///     all. Only the values the installer wrote are touched; an entry a plugin or an administrator gave
///     its own icon is left alone and is reported, because only its author knows what it should become.
/// </summary>
public class MigrationUpdateAdminSiteMapIcons : IMigration
{
    //the same table the views were converted with, Font Awesome 4 and simple-line-icons to bootstrap-icons
    private static readonly Dictionary<string, string> IconMap = new() {
        { "fa fa-dot-circle-o", "bi bi-record-circle" },
        { "fa fa-arrow-circle-o-right", "bi bi-arrow-right-circle" },
        { "fa fa-sitemap", "bi bi-diagram-3" },
        { "icon-home", "bi bi-house" },
        { "icon-bar-chart", "bi bi-bar-chart" },
        { "icon-bulb", "bi bi-lightbulb" },
        { "icon-basket", "bi bi-basket" },
        { "icon-users", "bi bi-people" },
        { "icon-layers", "bi bi-layers" },
        { "icon-settings", "bi bi-gear" },
        { "icon-wrench", "bi bi-wrench" },
        { "icon-puzzle", "bi bi-puzzle" },
        { "icon-info", "bi bi-info-circle" },
        { "icon-question", "bi bi-question-circle" }
    };

    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("4C1F9A73-2E58-4D0B-9F16-8A52C7B3E604");
    public string Name => "Move the admin menu icons from Font Awesome to bootstrap-icons 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateAdminSiteMapIcons>>();

        try
        {
            var unknown = new HashSet<string>();
            foreach (var node in repository.Table.ToList())
            {
                if (!Rewrite(node, unknown)) continue;
                repository.Update(node);
            }

            if (unknown.Count > 0)
                logService.LogInformation(
                    "UpdateAdminSiteMapIcons (2.4) - these menu icons are not standard and were left as they are, set them to a bootstrap-icons class in the admin area: {Icons}",
                    string.Join(", ", unknown));
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - UpdateAdminSiteMapIcons (2.4)");
        }

        return true;
    }

    /// <summary>
    ///     Rewrites one entry and everything below it. Returns whether anything changed.
    /// </summary>
    private static bool Rewrite(AdminSiteMap node, ISet<string> unknown)
    {
        var changed = false;
        if (!string.IsNullOrWhiteSpace(node.IconClass) && !node.IconClass.Contains("bi-"))
        {
            if (IconMap.TryGetValue(node.IconClass.Trim(), out var replacement))
            {
                node.IconClass = replacement;
                changed = true;
            }
            else
            {
                unknown.Add(node.IconClass.Trim());
            }
        }

        foreach (var child in node.ChildNodes ?? [])
            if (Rewrite(child, unknown))
                changed = true;

        return changed;
    }
}
