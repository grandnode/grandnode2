using AutoMapper;
using Grand.Domain.AdminSearch;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class AdminSearchSettingsProfile : Profile, IAutoMapperProfile
    {
        public AdminSearchSettingsProfile()
        {
            CreateMap<AdminSearchSettings, AdminSearchSettingsModel>();
            CreateMap<AdminSearchSettingsModel, AdminSearchSettings>();
        }

        public int Order => 0;
    }
}