using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Discounts;
using Grand.Web.Admin.Models.Discounts;

namespace Grand.Web.Admin.Extensions
{
    public static class DiscountMappingExtensions
    {
        public static DiscountModel ToModel(this Discount entity, IDateTimeService dateTimeService)
        {
            var discount = entity.MapTo<Discount, DiscountModel>();
            discount.StartDate = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
            discount.EndDate = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
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
}