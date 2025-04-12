using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class MessageTemplateMappingExtensions
{
    public static MessageTemplateModel ToModel(this MessageTemplate entity)
    {
        return entity.MapTo<MessageTemplate, MessageTemplateModel>();
    }

    public static MessageTemplate ToEntity(this MessageTemplateModel model)
    {
        return model.MapTo<MessageTemplateModel, MessageTemplate>();
    }

    public static MessageTemplate ToEntity(this MessageTemplateModel model, MessageTemplate destination)
    {
        return model.MapTo(destination);
    }
}