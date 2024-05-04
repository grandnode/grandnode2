using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class ContactUsMappingExtensions
{
    public static ContactFormModel ToModel(this ContactUs entity)
    {
        return entity.MapTo<ContactUs, ContactFormModel>();
    }
}