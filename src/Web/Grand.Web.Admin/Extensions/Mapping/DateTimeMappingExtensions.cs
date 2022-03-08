using Grand.Business.Common.Interfaces.Directory;

namespace Grand.Web.Admin.Extensions
{
    public static class DateTimeMappingExtensions
    {
        public static DateTime? ConvertToUserTime(this DateTime? datetime, IDateTimeService dateTimeService)
        {
            if (datetime.HasValue)
            {           
                if(datetime.Value.Kind == DateTimeKind.Utc)
                    datetime = dateTimeService.ConvertToUserTime(datetime.Value, TimeZoneInfo.Utc, dateTimeService.CurrentTimeZone);
            }
            return datetime;
        }

        public static DateTime? ConvertToUtcTime(this DateTime? datetime, IDateTimeService dateTimeService)
        {
            if (datetime.HasValue)
            {
                if (datetime.Value.Kind != DateTimeKind.Utc)
                    datetime = dateTimeService.ConvertToUtcTime(datetime.Value, dateTimeService.CurrentTimeZone);
            }
            return datetime;
        }

        public static DateTime ConvertToUserTime(this DateTime datetime, IDateTimeService dateTimeService)
        {
            if(datetime.Kind == DateTimeKind.Utc)
                return dateTimeService.ConvertToUserTime(datetime, TimeZoneInfo.Utc, dateTimeService.CurrentTimeZone);

            return datetime;
        }

        public static DateTime ConvertToUtcTime(this DateTime datetime, IDateTimeService dateTimeService)
        {
            if (datetime.Kind == DateTimeKind.Local)
                return dateTimeService.ConvertToUtcTime(datetime, dateTimeService.CurrentTimeZone);

            return datetime;
        }
    }
}