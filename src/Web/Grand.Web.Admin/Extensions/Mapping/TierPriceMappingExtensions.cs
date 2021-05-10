using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions
{
    public static class TierPriceMappingExtensions
    {
        public static ProductModel.TierPriceModel ToModel(this TierPrice entity, IDateTimeService dateTimeService)
        {
            var tierprice = entity.MapTo<TierPrice, ProductModel.TierPriceModel>();
            tierprice.StartDateTime = entity.StartDateTimeUtc.ConvertToUserTime(dateTimeService);
            tierprice.EndDateTime = entity.EndDateTimeUtc.ConvertToUserTime(dateTimeService);
            return tierprice;
        }

        public static TierPrice ToEntity(this ProductModel.TierPriceModel model, IDateTimeService dateTimeService)
        {
            var tierprice = model.MapTo<ProductModel.TierPriceModel, TierPrice>();
            tierprice.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
            tierprice.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
            return tierprice;
        }

        public static TierPrice ToEntity(this ProductModel.TierPriceModel model, TierPrice destination, IDateTimeService dateTimeService)
        {
            var tierprice = model.MapTo(destination);
            tierprice.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
            tierprice.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
            return tierprice;
        }
    }
}