using Grand.Data;
using Grand.Domain.Common;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Names the AI crawlers in robots.txt, as the installer now does for new stores: they are
///     added as User-agent lines of the existing "User-agent: *" group, so they keep exactly the
///     rules they already followed - nothing is allowed or blocked that was not before.
///     A robots.txt that already names any of them, or has no "*" group, is the operator's own
///     policy and is left untouched.
/// </summary>
public class MigrationRobotsTxtAiCrawlers : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("5B0E3C2A-8D41-4F6E-9A27-C1D84E6F3B95");
    public string Name => "Name the AI crawlers in robots.txt 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRobotsTxtAiCrawlers>>();
        try
        {
            var repository = serviceProvider.GetRequiredService<IRepository<RobotsTxt>>();
            foreach (var robotsTxt in repository.Table.ToList())
            {
                var text = AddAiCrawlers(robotsTxt.Text);
                if (text == robotsTxt.Text) continue;

                robotsTxt.Text = text;
                repository.Update(robotsTxt);
            }

            var cache = serviceProvider.GetService<ICacheBase>();
            cache?.RemoveByPrefix(CacheKey.ROBOTS_ALL_KEY).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RobotsTxtAiCrawlers (2.4)");
        }

        return true;
    }

    public static string AddAiCrawlers(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var lines = text.Split('\n').ToList();
        if (lines.Any(line => RobotsTxtAiCrawlers.UserAgents.Any(agent =>
                IsUserAgentLine(line, agent)))) return text;

        var wildcard = lines.FindIndex(line => IsUserAgentLine(line, "*"));
        if (wildcard < 0) return text;

        var lineEnd = lines[wildcard].EndsWith('\r') ? "\r" : "";
        lines.InsertRange(wildcard + 1, RobotsTxtAiCrawlers.UserAgents.Select(x => "User-agent: " + x + lineEnd));
        return string.Join('\n', lines);
    }

    private static bool IsUserAgentLine(string line, string agent)
    {
        var parts = line.Split(':', 2);
        return parts.Length == 2
               && parts[0].Trim().Equals("User-agent", StringComparison.OrdinalIgnoreCase)
               && parts[1].Trim().Equals(agent, StringComparison.OrdinalIgnoreCase);
    }
}
