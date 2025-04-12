using Grand.Domain.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Directory;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class MeasureDimensionMappingExtensions
{
    public static MeasureDimensionModel ToModel(this MeasureDimension entity)
    {
        return entity.MapTo<MeasureDimension, MeasureDimensionModel>();
    }

    public static MeasureDimension ToEntity(this MeasureDimensionModel model)
    {
        return model.MapTo<MeasureDimensionModel, MeasureDimension>();
    }

    public static MeasureDimension ToEntity(this MeasureDimensionModel model, MeasureDimension destination)
    {
        return model.MapTo(destination);
    }
}