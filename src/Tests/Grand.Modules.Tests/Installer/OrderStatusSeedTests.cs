using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Installer;

/// <summary>
///     New installations get the status that orders awaiting approval use and the default list of payment
///     methods allowed for them. Read from the installer source, like the other seed guards in this folder.
/// </summary>
[TestClass]
public class OrderStatusSeedTests
{
    [TestMethod]
    public void InstallOrderStatus_SeedsTheAwaitingApprovalSystemStatus()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPaths.InstallerServices, "InstallDataOrderStatus.cs"));
        var seed = Regex.Match(source,
            @"new\(\)\s*\{(?<body>[^}]*StatusId\s*=\s*5\b[^}]*)\}", RegexOptions.Singleline);

        Assert.IsTrue(seed.Success, "status 5 not seeded in InstallDataOrderStatus.cs");
        var body = seed.Groups["body"].Value;
        StringAssert.Matches(body, new Regex(@"IsSystem\s*=\s*true"));
        StringAssert.Matches(body, new Regex(@"Name\s*=\s*""Awaiting approval"""));
        StringAssert.Matches(body, new Regex(@"DisplayOrder\s*=\s*5\b"));
    }

    [TestMethod]
    public void InstallSettings_SeedsCashOnDeliveryForOrdersAwaitingApproval()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPaths.InstallerServices, "InstallDataSettings.cs"));
        var seed = Regex.Match(source, @"new OrderApprovalSettings\s*\{(?<body>.*?)\}\);", RegexOptions.Singleline);

        Assert.IsTrue(seed.Success, "OrderApprovalSettings seed not found in InstallDataSettings.cs");
        StringAssert.Matches(seed.Groups["body"].Value,
            new Regex(@"PaymentMethodSystemNames\s*=\s*\[\s*""Payments\.CashOnDelivery""\s*\]"));
    }
}
