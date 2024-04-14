using Grand.Domain.Admin;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Menu;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class MenuMappingExtensions
{
    public static MenuModel ToModel(this AdminSiteMap entity)
    {
        return entity.MapTo<AdminSiteMap, MenuModel>();
    }

    public static AdminSiteMap ToEntity(this MenuModel model)
    {
        return model.MapTo<MenuModel, AdminSiteMap>();
    }

    public static AdminSiteMap ToEntity(this MenuModel model, AdminSiteMap destination)
    {
        return model.MapTo(destination);
    }
}