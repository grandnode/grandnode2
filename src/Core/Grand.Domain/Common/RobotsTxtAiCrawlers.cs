namespace Grand.Domain.Common;

/// <summary>
///     The AI crawlers the default robots.txt names explicitly. They are listed as extra
///     User-agent lines of the "*" group, so they get exactly the rules every other crawler
///     gets - the point is that the policy towards them is stated, not implied.
///     A crawler obeys only the most specific group that names it, which is why they are
///     not given a group of their own: that would silently drop the Disallow rules for them.
/// </summary>
public static class RobotsTxtAiCrawlers
{
    public static readonly string[] UserAgents = [
        "GPTBot",
        "OAI-SearchBot",
        "ChatGPT-User",
        "ClaudeBot",
        "Claude-SearchBot",
        "Claude-User",
        "PerplexityBot",
        "Perplexity-User",
        "Google-Extended",
        "Applebot-Extended",
        "meta-externalagent",
        "Amazonbot",
        "CCBot"
    ];

    public static string UserAgentLines =>
        string.Join("\n", UserAgents.Select(x => "User-agent: " + x));
}
