using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Web.AdminShared.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.ObjectModel;

namespace Grand.Web.Admin.Tests.Extensions;

[TestClass]
public class DateTimeServiceExtensionsTests
{
    private const string ServerLabel = "Server time zone";

    private readonly TimeZoneInfo _dateline = TimeZoneInfo.CreateCustomTimeZone("Dateline", TimeSpan.FromHours(-12), "Dateline", "Dateline");
    private readonly TimeZoneInfo _warsaw = TimeZoneInfo.CreateCustomTimeZone("Warsaw", TimeSpan.FromHours(1), "Warsaw", "Warsaw");
    private readonly TimeZoneInfo _unlisted = TimeZoneInfo.CreateCustomTimeZone("Other/Name", TimeSpan.FromHours(2), "Other", "Other");
    private IDateTimeService _dateTimeService;

    [TestInitialize]
    public void Init()
    {
        var service = new Mock<IDateTimeService>();
        service.Setup(x => x.GetSystemTimeZones())
            .Returns(new ReadOnlyCollection<TimeZoneInfo>([_dateline, _warsaw]));
        service.Setup(x => x.FindTimeZoneById(It.IsAny<string>())).Throws<TimeZoneNotFoundException>();
        service.Setup(x => x.FindTimeZoneById("Other/Name")).Returns(_unlisted);
        _dateTimeService = service.Object;
    }

    private IList<SelectListItem> Options(string configured) =>
        _dateTimeService.GetStoreTimeZoneOptions(configured, ServerLabel);

    private static string Selected(IList<SelectListItem> options) => options.Single(x => x.Selected).Value;

    [TestMethod]
    public void GetStoreTimeZoneOptions_StartWithTheServerZone_WithAnEmptyValue()
    {
        var first = Options("Warsaw")[0];

        Assert.AreEqual("", first.Value);
        StringAssert.StartsWith(first.Text, ServerLabel);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void GetStoreTimeZoneOptions_NothingConfigured_SelectsTheServerZone(string configured)
    {
        //so saving the form leaves the store following the server
        Assert.AreEqual("", Selected(Options(configured)));
    }

    [TestMethod]
    public void GetStoreTimeZoneOptions_ConfiguredZoneListed_SelectsIt()
    {
        Assert.AreEqual("Warsaw", Selected(Options("Warsaw")));
    }

    [TestMethod]
    public void GetStoreTimeZoneOptions_ConfiguredZoneDiffersInCase_SelectsTheListedId()
    {
        Assert.AreEqual("Warsaw", Selected(Options("warsaw")));
    }

    [TestMethod]
    public void GetStoreTimeZoneOptions_ConfiguredZoneResolvesButIsNotListed_KeepsIt()
    {
        //a Windows id on Linux, or the reverse: dates are shown in it, so saving must not change it
        Assert.AreEqual("Other/Name", Selected(Options("Other/Name")));
    }

    [TestMethod]
    public void GetStoreTimeZoneOptions_ConfiguredZoneUnknownOnThisMachine_SelectsTheServerZone()
    {
        //DateTimeService.CurrentTimeZone falls back to the server here as well
        Assert.AreEqual("", Selected(Options("Gone/Zone")));
    }
}
