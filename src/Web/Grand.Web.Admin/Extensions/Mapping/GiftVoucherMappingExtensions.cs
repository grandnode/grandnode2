using Grand.Infrastructure.Mapper;
using Grand.Domain.Orders;
using Grand.Web.Admin.Models.Orders;
using Grand.Business.Common.Interfaces.Directory;

namespace Grand.Web.Admin.Extensions
{
    public static class GiftVoucherMappingExtensions
    {
        public static GiftVoucherModel ToModel(this GiftVoucher entity, IDateTimeService dateTimeService)
        {
            var giftVoucher = entity.MapTo<GiftVoucher, GiftVoucherModel>();
            giftVoucher.ValidTo = entity.ValidTo.ConvertToUserTime(dateTimeService);
            return giftVoucher;
        }

        public static GiftVoucher ToEntity(this GiftVoucherModel model, IDateTimeService dateTimeService)
        {
            var giftVoucher = model.MapTo<GiftVoucherModel, GiftVoucher>();
            giftVoucher.ValidTo = model.ValidTo.ConvertToUtcTime(dateTimeService);
            return giftVoucher;
        }

        public static GiftVoucher ToEntity(this GiftVoucherModel model, GiftVoucher destination, IDateTimeService dateTimeService)
        {
            var giftVoucher = model.MapTo(destination);
            giftVoucher.ValidTo = model.ValidTo.ConvertToUtcTime(dateTimeService);
            return giftVoucher;
        }
    }
}