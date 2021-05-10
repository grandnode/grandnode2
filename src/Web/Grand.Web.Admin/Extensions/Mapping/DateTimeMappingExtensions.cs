using Grand.Business.Common.Interfaces.Directory;
using System;

namespace Grand.Web.Admin.Extensions
{
    public static class DateTimeMappingExtensions
    {
        public static DateTime? ConvertToUserTime(this DateTime? datetime, IDateTimeService dateTimeService)
        {
            if (datetime.HasValue)
            {
                datetime = dateTimeService.ConvertToUserTime(datetime.Value, TimeZoneInfo.Utc, dateTimeService.CurrentTimeZone);
            }
            return datetime;
        }

        public static DateTime? ConvertToUtcTime(this DateTime? datetime, IDateTimeService dateTimeService)
        {
            if (datetime.HasValue)
            {
                datetime = dateTimeService.ConvertToUtcTime(datetime.Value, dateTimeService.CurrentTimeZone);
            }
            return datetime;
        }

        public static DateTime ConvertToUserTime(this DateTime datetime, IDateTimeService dateTimeService)
        {
            return dateTimeService.ConvertToUserTime(datetime, TimeZoneInfo.Utc, dateTimeService.CurrentTimeZone);
        }

        public static DateTime ConvertToUtcTime(this DateTime datetime, IDateTimeService dateTimeService)
        {
            return dateTimeService.ConvertToUtcTime(datetime, dateTimeService.CurrentTimeZone);
        }
    }
}