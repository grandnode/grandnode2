using AutoMapper;
using Grand.Domain.Admin;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Mapper;

public class AdminSearchSettingsProfile : Profile, IAutoMapperProfile
{
    public AdminSearchSettingsProfile()
    {
        CreateMap<AdminSearchSettings, AdminSearchSettingsModel>();
        CreateMap<AdminSearchSettingsModel, AdminSearchSettings>();
    }

    public int Order => 0;
}