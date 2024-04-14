using Grand.Domain.Permissions;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Permissions;

namespace Grand.Web.Admin.Extensions.Mapping;

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