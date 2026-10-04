using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class MeasureWeightMappingExtensions
{
    public static MeasureWeightModel ToModel(this MeasureWeight entity)
    {
        return entity.MapTo<MeasureWeight, MeasureWeightModel>();
    }

    public static MeasureWeight ToEntity(this MeasureWeightModel model)
    {
        return model.MapTo<MeasureWeightModel, MeasureWeight>();
    }

    public static MeasureWeight ToEntity(this MeasureWeightModel model, MeasureWeight destination)
    {
        return model.MapTo(destination);
    }
}