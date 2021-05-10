using Grand.Infrastructure.Mapper;
using Grand.Domain.Directory;
using Grand.Web.Admin.Models.Directory;

namespace Grand.Web.Admin.Extensions
{
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
}