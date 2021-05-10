using AutoMapper;
using Grand.Infrastructure.Mapper;
using Shipping.ShippingPoint.Domain;
using Shipping.ShippingPoint.Models;

namespace Shipping.ShippingPoint
{
    public class MapperConfiguration : Profile, IAutoMapperProfile
    {
        public int Order
        {
            get { return 0; }
        }

        public MapperConfiguration()
        {
            CreateMap<ShippingPoints, ShippingPointModel>()
            .ForMember(dest => dest.AvailableStores, mo => mo.Ignore())
            .ForMember(dest => dest.StoreName, mo => mo.Ignore())
            .ForMember(dest => dest.AvailableCountries, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<ShippingPointModel, ShippingPoints>();
        }
    }
}
