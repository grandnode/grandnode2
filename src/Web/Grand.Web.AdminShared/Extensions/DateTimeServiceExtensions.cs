using Grand.Business.Core.Interfaces.Common.Directory;

namespace Grand.Web.AdminShared.Extensions;

public static class DateTimeServiceExtensions
{
    /// <summary>
    ///     The time zone dates are really shown in: the configured one, or the server's when none is set or the
    ///     machine does not know it (see DateTimeService.CurrentTimeZone). A form has to preselect this one -
    ///     a select with nothing matching shows its first option, and saving would then store that zone.
    /// </summary>
    public static string GetEffectiveTimeZoneId(this IDateTimeService dateTimeService, string configuredTimeZoneId)
    {
        var configured = dateTimeService.GetSystemTimeZones()
            .FirstOrDefault(x => x.Id.Equals(configuredTimeZoneId, StringComparison.OrdinalIgnoreCase));

        return configured?.Id ?? TimeZoneInfo.Local.Id;
    }
}
