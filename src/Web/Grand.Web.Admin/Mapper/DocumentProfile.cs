using AutoMapper;
using Grand.Domain.Documents;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Documents;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class DocumentProfile : Profile, IAutoMapperProfile
    {
        public DocumentProfile()
        {
            CreateMap<Document, DocumentModel>()
               .ForMember(dest => dest.AvailableDocumentTypes, mo => mo.Ignore())
               .ForMember(dest => dest.AvailableSelesEmployees, mo => mo.Ignore());
            CreateMap<DocumentModel, Document>()
               .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
               .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
               .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
               .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()))
               .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}