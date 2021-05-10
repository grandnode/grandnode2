using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Mapper
{
    public class QueuedEmailProfile : Profile, IAutoMapperProfile
    {
        public QueuedEmailProfile()
        {
            CreateMap<QueuedEmail, QueuedEmailModel>()
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore())
                .ForMember(dest => dest.PriorityName, mo => mo.Ignore())
                .ForMember(dest => dest.DontSendBeforeDate, mo => mo.Ignore())
                .ForMember(dest => dest.SendImmediately, mo => mo.Ignore())
                .ForMember(dest => dest.SentOn, mo => mo.Ignore())
                .ForMember(dest => dest.ReadOn, mo => mo.Ignore());

            CreateMap<QueuedEmailModel, QueuedEmail>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, dt => dt.Ignore())
                .ForMember(dest => dest.DontSendBeforeDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.SentOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.ReadOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.EmailAccountId, mo => mo.Ignore())
                .ForMember(dest => dest.AttachmentFilePath, mo => mo.Ignore())
                .ForMember(dest => dest.AttachmentFileName, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}