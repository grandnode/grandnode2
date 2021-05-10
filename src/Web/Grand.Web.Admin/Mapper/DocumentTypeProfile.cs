using AutoMapper;
using Grand.Domain.Documents;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Documents;

namespace Grand.Web.Admin.Mapper
{
    public class DocumentTypeProfile : Profile, IAutoMapperProfile
    {
        public DocumentTypeProfile()
        {
            CreateMap<DocumentType, DocumentTypeModel>();
            CreateMap<DocumentTypeModel, DocumentType>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}