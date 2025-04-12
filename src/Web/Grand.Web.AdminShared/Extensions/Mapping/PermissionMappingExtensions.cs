using Grand.Domain.Permissions;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Permissions;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class PermissionMappingExtensions
{
    public static Permission ToEntity(this PermissionCreateModel model)
    {
        return model.MapTo<PermissionCreateModel, Permission>();
    }

    public static PermissionUpdateModel ToModel(this Permission entity)
    {
        return entity.MapTo<Permission, PermissionUpdateModel>();
    }

    public static Permission ToEntity(this PermissionUpdateModel model, Permission destination)
    {
        return model.MapTo(destination);
    }
}