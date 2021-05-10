using Grand.Infrastructure.Mapper;
using Grand.Domain.Orders;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class ShoppingCartSettingsMappingExtensions
    {
        public static SalesSettingsModel.ShoppingCartSettingsModel ToModel(this ShoppingCartSettings entity)
        {
            return entity.MapTo<ShoppingCartSettings, SalesSettingsModel.ShoppingCartSettingsModel>();
        }
        public static ShoppingCartSettings ToEntity(this SalesSettingsModel.ShoppingCartSettingsModel model, ShoppingCartSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}