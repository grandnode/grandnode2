using AutoMapper;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Tasks;

namespace Grand.Web.AdminShared.Mapper;

public class ScheduleTaskProfile : Profile, IAutoMapperProfile
{
    public ScheduleTaskProfile()
    {
        CreateMap<ScheduleTask, ScheduleTaskModel>()
            .ForMember(dest => dest.AvailableStores, mo => mo.Ignore());

        CreateMap<ScheduleTaskModel, ScheduleTask>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.ScheduleTaskName, mo => mo.Ignore())
            .ForMember(dest => dest.LastNonSuccessEndUtc, mo => mo.Ignore())
            .ForMember(dest => dest.LastStartUtc, mo => mo.Ignore())
            .ForMember(dest => dest.LeasedUntilUtc, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore())
            .ForMember(dest => dest.LastSuccessUtc, mo => mo.Ignore());
    }

    public int Order => 0;
}