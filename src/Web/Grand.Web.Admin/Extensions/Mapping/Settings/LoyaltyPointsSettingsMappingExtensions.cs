using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

public static class LoyaltyPointsSettingsMappingExtensions
{
    public static SalesSettingsModel.LoyaltyPointsSettingsModel ToModel(this LoyaltyPointsSettings entity)
    {
        return entity.MapTo<LoyaltyPointsSettings, SalesSettingsModel.LoyaltyPointsSettingsModel>();
    }

    public static LoyaltyPointsSettings ToEntity(this SalesSettingsModel.LoyaltyPointsSettingsModel model,
        LoyaltyPointsSettings destination)
    {
        return model.MapTo(destination);
    }
}