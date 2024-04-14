using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Discounts;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Discounts;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class DiscountMappingExtensions
{
    public static DiscountModel ToModel(this Discount entity, IDateTimeService dateTimeService)
    {
        var discount = entity.MapTo<Discount, DiscountModel>();
        discount.StartDate = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
        discount.EndDate = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
        return discount;
    }

    public static DiscountModel ToModel(this Discount entity)
    {
        var discount = entity.MapTo<Discount, DiscountModel>();
        return discount;
    }

    public static Discount ToEntity(this DiscountModel model, IDateTimeService dateTimeService)
    {
        var discount = model.MapTo<DiscountModel, Discount>();
        discount.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
        discount.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
        return discount;
    }

    public static Discount ToEntity(this DiscountModel model, Discount destination, IDateTimeService dateTimeService)
    {
        var discount = model.MapTo(destination);
        discount.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
        discount.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
        return discount;
    }
}