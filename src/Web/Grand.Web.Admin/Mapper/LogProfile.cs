using AutoMapper;
using Grand.Domain.Logging;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Logging;

namespace Grand.Web.Admin.Mapper
{
    public class LogProfile : Profile, IAutoMapperProfile
    {
        public LogProfile()
        {
            CreateMap<Log, LogModel>()
                .ForMember(dest => dest.CustomerEmail, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore());

            CreateMap<LogModel, Log>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}