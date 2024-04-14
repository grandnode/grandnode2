using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class MerchandiseReturnActionMappingExtensions
{
    public static MerchandiseReturnActionModel ToModel(this MerchandiseReturnAction entity)
    {
        return entity.MapTo<MerchandiseReturnAction, MerchandiseReturnActionModel>();
    }

    public static MerchandiseReturnAction ToEntity(this MerchandiseReturnActionModel model)
    {
        return model.MapTo<MerchandiseReturnActionModel, MerchandiseReturnAction>();
    }

    public static MerchandiseReturnAction ToEntity(this MerchandiseReturnActionModel model,
        MerchandiseReturnAction destination)
    {
        return model.MapTo(destination);
    }
}