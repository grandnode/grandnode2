using Grand.Business.Core.Interfaces.Common.Directory;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Extensions;

public static class DateTimeServiceExtensions
{
    /// <summary>
    ///     The choices for a store's time zone, with the one dates are really shown in selected
    ///     (see DateTimeService.CurrentTimeZone). The empty value means the store follows the server, as it does
    ///     after installation, so saving the form never turns that into a fixed zone. A configured zone that
    ///     resolves but is not listed - a Windows id on Linux, or the reverse - keeps an option of its own.
    /// </summary>
    public static IList<SelectListItem> GetStoreTimeZoneOptions(this IDateTimeService dateTimeService,
        string configuredTimeZoneId, string serverTimeZoneLabel)
    {
        var options = new List<SelectListItem> {
            new() { Text = $"{serverTimeZoneLabel}: {DisplayName(TimeZoneInfo.Local)}", Value = "" }
        };
        options.AddRange(dateTimeService.GetSystemTimeZones()
            .Select(zone => new SelectListItem { Text = DisplayName(zone), Value = zone.Id }));

        var selected = options.FirstOrDefault(x => x.Value.Equals(configuredTimeZoneId, StringComparison.OrdinalIgnoreCase))
                       ?? AddUnlistedZone(dateTimeService, configuredTimeZoneId, options)
                       ?? options[0];
        selected.Selected = true;

        return options;
    }

    private static SelectListItem AddUnlistedZone(IDateTimeService dateTimeService, string timeZoneId,
        List<SelectListItem> options)
    {
        if (string.IsNullOrEmpty(timeZoneId)) return null;
        try
        {
            var option = new SelectListItem {
                Text = DisplayName(dateTimeService.FindTimeZoneById(timeZoneId)), Value = timeZoneId
            };
            options.Add(option);
            return option;
        }
        catch (Exception exc) when (exc is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            //unknown on this machine - DateTimeService.CurrentTimeZone shows dates in the server's zone then
            return null;
        }
    }

    private static string DisplayName(TimeZoneInfo zone)
    {
        return Grand.Infrastructure.OperatingSystem.IsWindows()
            ? zone.DisplayName
            : $"{zone.StandardName} ({zone.Id})";
    }
}
