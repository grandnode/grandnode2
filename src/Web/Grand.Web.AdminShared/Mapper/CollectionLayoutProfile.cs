using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Layouts;

namespace Grand.Web.AdminShared.Mapper;

public class CollectionLayoutProfile : Profile, IAutoMapperProfile
{
    public CollectionLayoutProfile()
    {
        CreateMap<CollectionLayout, CollectionLayoutModel>();
        CreateMap<CollectionLayoutModel, CollectionLayout>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}