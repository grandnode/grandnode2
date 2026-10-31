using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Web.AdminShared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.ObjectModel;

namespace Grand.Web.Admin.Tests.Extensions;

[TestClass]
public class DateTimeServiceExtensionsTests
{
    private readonly TimeZoneInfo _dateline = TimeZoneInfo.CreateCustomTimeZone("Dateline", TimeSpan.FromHours(-12), "Dateline", "Dateline");
    private readonly TimeZoneInfo _warsaw = TimeZoneInfo.CreateCustomTimeZone("Warsaw", TimeSpan.FromHours(1), "Warsaw", "Warsaw");
    private IDateTimeService _dateTimeService;

    [TestInitialize]
    public void Init()
    {
        var service = new Mock<IDateTimeService>();
        service.Setup(x => x.GetSystemTimeZones())
            .Returns(new ReadOnlyCollection<TimeZoneInfo>([_dateline, _warsaw, TimeZoneInfo.Local]));
        _dateTimeService = service.Object;
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void GetEffectiveTimeZoneId_NothingConfigured_ReturnsTheServerZone(string configured)
    {
        Assert.AreEqual(TimeZoneInfo.Local.Id, _dateTimeService.GetEffectiveTimeZoneId(configured));
    }

    [TestMethod]
    public void GetEffectiveTimeZoneId_ConfiguredZoneExists_ReturnsIt()
    {
        Assert.AreEqual("Warsaw", _dateTimeService.GetEffectiveTimeZoneId("Warsaw"));
    }

    [TestMethod]
    public void GetEffectiveTimeZoneId_ConfiguredZoneDiffersInCase_ReturnsTheListedId()
    {
        Assert.AreEqual("Warsaw", _dateTimeService.GetEffectiveTimeZoneId("warsaw"));
    }

    [TestMethod]
    public void GetEffectiveTimeZoneId_ConfiguredZoneUnknownOnThisMachine_ReturnsTheServerZone()
    {
        Assert.AreEqual(TimeZoneInfo.Local.Id, _dateTimeService.GetEffectiveTimeZoneId("Gone/Zone"));
    }
}
