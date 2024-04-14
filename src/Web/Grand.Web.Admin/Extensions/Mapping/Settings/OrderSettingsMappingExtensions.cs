using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

public static class OrderSettingsMappingExtensions
{
    public static SalesSettingsModel.OrderSettingsModel ToModel(this OrderSettings entity)
    {
        return entity.MapTo<OrderSettings, SalesSettingsModel.OrderSettingsModel>();
    }

    public static OrderSettings ToEntity(this SalesSettingsModel.OrderSettingsModel model, OrderSettings destination)
    {
        return model.MapTo(destination);
    }
}