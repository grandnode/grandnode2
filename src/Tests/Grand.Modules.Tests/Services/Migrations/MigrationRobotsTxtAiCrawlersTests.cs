using Grand.Domain.Common;
using Grand.Module.Migration.Migrations._2._4;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Services.Migrations;

[TestClass]
public class MigrationRobotsTxtAiCrawlersTests
{
    [TestMethod]
    public void AddAiCrawlers_JoinsTheWildcardGroup()
    {
        var text = "User-agent: *\r\nDisallow: /admin\r\nDisallow: /cart";

        var result = MigrationRobotsTxtAiCrawlers.AddAiCrawlers(text);

        var lines = result.Split("\r\n");
        Assert.AreEqual("User-agent: *", lines[0]);
        CollectionAssert.AreEqual(RobotsTxtAiCrawlers.UserAgents.Select(x => "User-agent: " + x).ToArray(),
            lines.Skip(1).Take(RobotsTxtAiCrawlers.UserAgents.Length).ToArray());
        //the rules follow the whole group, so the named crawlers keep them
        Assert.AreEqual("Disallow: /admin", lines[RobotsTxtAiCrawlers.UserAgents.Length + 1]);
    }

    [TestMethod]
    [DataRow("User-agent: *\nDisallow: /\n\nUser-agent: GPTBot\nDisallow: /")]
    [DataRow("user-agent: claudebot\nAllow: /")]
    [DataRow("User-agent: Googlebot\nDisallow: /cart")]
    [DataRow("")]
    public void AddAiCrawlers_LeavesAnExplicitPolicyAlone(string text)
    {
        Assert.AreEqual(text, MigrationRobotsTxtAiCrawlers.AddAiCrawlers(text));
    }
}
