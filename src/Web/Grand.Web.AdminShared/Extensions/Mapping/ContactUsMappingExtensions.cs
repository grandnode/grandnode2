using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class ContactUsMappingExtensions
{
    public static ContactFormModel ToModel(this ContactUs entity)
    {
        return entity.MapTo<ContactUs, ContactFormModel>();
    }
}