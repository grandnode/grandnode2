using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class UserApiMappingExtensions
{
    public static UserApiModel ToModel(this UserApi entity)
    {
        return entity.MapTo<UserApi, UserApiModel>();
    }

    public static UserApi ToEntity(this UserApiCreateModel model)
    {
        return model.MapTo<UserApiCreateModel, UserApi>();
    }

    public static UserApi ToEntity(this UserApiModel model, UserApi destination)
    {
        return model.MapTo(destination);
    }
}