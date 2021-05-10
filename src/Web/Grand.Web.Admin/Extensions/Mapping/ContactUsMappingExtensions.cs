using Grand.Infrastructure.Mapper;
using Grand.Domain.Messages;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Extensions
{
    public static class ContactUsMappingExtensions
    {
        public static ContactFormModel ToModel(this ContactUs entity)
        {
            return entity.MapTo<ContactUs, ContactFormModel>();
        }
    }
}