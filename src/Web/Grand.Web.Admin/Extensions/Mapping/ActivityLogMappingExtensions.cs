using Grand.Domain.Logging;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.ActivityLog;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class ActivityLogMappingExtensions
{
    public static ActivityLogTypeModel ToModel(this ActivityLogType entity)
    {
        return entity.MapTo<ActivityLogType, ActivityLogTypeModel>();
    }

    public static ActivityLogModel ToModel(this ActivityLog entity)
    {
        return entity.MapTo<ActivityLog, ActivityLogModel>();
    }

    public static ActivityStatsModel ToModel(this ActivityStats entity)
    {
        return entity.MapTo<ActivityStats, ActivityStatsModel>();
    }
}