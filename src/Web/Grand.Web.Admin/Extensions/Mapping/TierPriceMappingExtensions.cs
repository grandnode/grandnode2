using AutoMapper;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class TierPriceMappingExtensions
{
    public static ProductModel.TierPriceModel ToModel(this IMapper mapper, TierPrice entity, IDateTimeService dateTimeService)
    {
        var tierPrice = mapper.Map<ProductModel.TierPriceModel>(entity); 
        tierPrice.StartDateTime = entity.StartDateTimeUtc.ConvertToUserTime(dateTimeService);
        tierPrice.EndDateTime = entity.EndDateTimeUtc.ConvertToUserTime(dateTimeService);
        return tierPrice;
    }
    public static TierPrice ToEntity(this IMapper mapper, ProductModel.TierPriceModel model, IDateTimeService dateTimeService)
    {
        var tierPrice = mapper.Map<ProductModel.TierPriceModel, TierPrice>(model);
        tierPrice.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
        tierPrice.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
        return tierPrice;
    }
    public static TierPrice ToEntity(this IMapper mapper, ProductModel.TierPriceModel model, TierPrice destination,
        IDateTimeService dateTimeService)
    {
        var tierPrice = mapper.Map(model, destination);
        tierPrice.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
        tierPrice.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
        return tierPrice;
    }
}