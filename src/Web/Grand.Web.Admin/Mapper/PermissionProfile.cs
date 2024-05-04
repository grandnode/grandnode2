using AutoMapper;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Permissions;

namespace Grand.Web.Admin.Mapper;

public class PermissionProfile : Profile, IAutoMapperProfile
{
    public PermissionProfile()
    {
        CreateMap<PermissionCreateModel, Permission>();
        CreateMap<PermissionUpdateModel, Permission>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.SystemName, mo => mo.Ignore())
            .ForMember(dest => dest.Actions, mo => mo.Ignore())
            .ForMember(dest => dest.CustomerGroups, mo => mo.Ignore());

        CreateMap<Permission, PermissionUpdateModel>();
    }

    public int Order => 0;
}