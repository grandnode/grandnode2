using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Mapper
{
    public class TierPriceProfile : Profile, IAutoMapperProfile
    {
        public TierPriceProfile()
        {
            CreateMap<TierPrice, ProductModel.TierPriceModel>()
                .ForMember(dest => dest.AvailableStores, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCurrencies, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableCustomerGroups, mo => mo.Ignore());

            CreateMap<ProductModel.TierPriceModel, TierPrice>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}