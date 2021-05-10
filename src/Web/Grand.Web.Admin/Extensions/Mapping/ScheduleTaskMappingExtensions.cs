using Grand.Infrastructure.Mapper;
using Grand.Domain.Tasks;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Web.Admin.Models.Tasks;

namespace Grand.Web.Admin.Extensions.Mapping
{
    public static class ScheduleTaskMappingExtensions
    {
        public static ScheduleTaskModel ToModel(this ScheduleTask entity, IDateTimeService dateTimeService)
        {
            var taskModel = entity.MapTo<ScheduleTask, ScheduleTaskModel>();
            taskModel.LastStartUtc = entity.LastStartUtc.ConvertToUserTime(dateTimeService);
            taskModel.LastSuccessUtc = entity.LastSuccessUtc.ConvertToUserTime(dateTimeService);
            taskModel.LastEndUtc = entity.LastNonSuccessEndUtc.ConvertToUserTime(dateTimeService);
            return taskModel;

        }

        public static ScheduleTask ToEntity(this ScheduleTaskModel model)
        {
            return model.MapTo<ScheduleTaskModel, ScheduleTask>();
        }

        public static ScheduleTask ToEntity(this ScheduleTaskModel model, ScheduleTask destination)
        {
            return model.MapTo(destination);
        }
    }
}
