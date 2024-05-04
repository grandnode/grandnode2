using Grand.Business.Common.Services.Directory;
using Grand.Domain.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Common.Tests.Services.Directory;

[TestClass]
public class DateTimeServiceTests
{
    private DateTimeService _dateTimeService;
    private DateTimeSettings _dateTimeSettings;
    private TimeZoneInfo _timeZone;

    [TestInitialize]
    public void TestInitialize()
    {
        _timeZone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.BaseUtcOffset.Hours == 1);

        _dateTimeSettings = new DateTimeSettings {
            DefaultStoreTimeZoneId = _timeZone.Id
        };
        _dateTimeService = new DateTimeService(_dateTimeSettings);
    }

    [TestMethod]
    public void FindTimeZoneByIdTest()
    {
        var timezone = _dateTimeService.FindTimeZoneById(_dateTimeSettings.DefaultStoreTimeZoneId);
        Assert.IsNotNull(timezone);
    }

    [TestMethod]
    public void GetSystemTimeZonesTest()
    {
        var timezones = _dateTimeService.GetSystemTimeZones();
        Assert.IsTrue(timezones.Any());
    }

    [TestMethod]
    [Ignore]
    public void ConvertToUserTimeTest()
    {
        var usertime = _dateTimeService.ConvertToUserTime(new DateTime(01, 01, 01, 01, 00, 00, DateTimeKind.Utc));
        Assert.IsNotNull(usertime);
        //TODO Assert.AreEqual(new DateTime(01, 01, 01, 02, 00, 00), usertime);
    }

    [TestMethod]
    [Ignore]
    public void ConvertToUtcTimeTest()
    {
        var usertime = _dateTimeService.ConvertToUtcTime(new DateTime(01, 01, 01, 02, 00, 00), _timeZone);
        Assert.IsNotNull(usertime);
        //TODO Assert.AreEqual(new DateTime(01, 01, 01, 01, 00, 00), usertime);
    }
}