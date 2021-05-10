using Grand.Infrastructure.Mapper;
using Grand.Domain.Orders;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class MerchandiseReturnReasonMappingExtensions
    {
        public static MerchandiseReturnReasonModel ToModel(this MerchandiseReturnReason entity)
        {
            return entity.MapTo<MerchandiseReturnReason, MerchandiseReturnReasonModel>();
        }

        public static MerchandiseReturnReason ToEntity(this MerchandiseReturnReasonModel model)
        {
            return model.MapTo<MerchandiseReturnReasonModel, MerchandiseReturnReason>();
        }

        public static MerchandiseReturnReason ToEntity(this MerchandiseReturnReasonModel model, MerchandiseReturnReason destination)
        {
            return model.MapTo(destination);
        }
    }
}