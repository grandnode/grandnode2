using AutoMapper;
using Grand.Api.DTOs.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using System;
using System.Linq;

namespace Grand.Api.Infrastructure.Mapper
{
    public class CustomerProfile : Profile, IAutoMapperProfile
    {
        public CustomerProfile()
        {
            CreateMap<CustomerDto, Customer>()
                .ForMember(dest => dest.Addresses, mo => mo.Ignore())
                .ForMember(dest => dest.CannotLoginUntilDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.BillingAddress, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.Groups, mo => mo.Ignore())
                .ForMember(dest => dest.CustomerTags, mo => mo.Ignore())
                .ForMember(dest => dest.FailedLoginAttempts, mo => mo.Ignore())
                .ForMember(dest => dest.HasContributions, mo => mo.Ignore())
                .ForMember(dest => dest.IsSystemAccount, mo => mo.Ignore())
                .ForMember(dest => dest.LastActivityDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LastIpAddress, mo => mo.Ignore())
                .ForMember(dest => dest.LastLoginDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LastPurchaseDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LastUpdateCartDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.LastUpdateWishListDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.Password, mo => mo.Ignore())
                .ForMember(dest => dest.PasswordChangeDateUtc, mo => mo.Ignore())
                .ForMember(dest => dest.PasswordFormatId, mo => mo.Ignore())
                .ForMember(dest => dest.PasswordSalt, mo => mo.Ignore())
                .ForMember(dest => dest.ShippingAddress, mo => mo.Ignore())
                .ForMember(dest => dest.ShoppingCartItems, mo => mo.Ignore())
                .ForMember(dest => dest.SystemName, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<Customer, CustomerDto>()
                .ForMember(dest => dest.FirstName, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName, "")))
                .ForMember(dest => dest.LastName, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName, "")))
                .ForMember(dest => dest.City, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.City, "")))
                .ForMember(dest => dest.Company, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Company, "")))
                .ForMember(dest => dest.DateOfBirth, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<DateTime?>(SystemCustomerFieldNames.DateOfBirth, "")))
                .ForMember(dest => dest.Fax, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Fax, "")))
                .ForMember(dest => dest.Gender, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Gender, "")))
                .ForMember(dest => dest.Phone, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.Phone, "")))
                .ForMember(dest => dest.StateProvinceId, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StateProvinceId, "")))
                .ForMember(dest => dest.StreetAddress, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress, "")))
                .ForMember(dest => dest.StreetAddress2, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.StreetAddress2, "")))
                .ForMember(dest => dest.VatNumber, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumber, "")))
                .ForMember(dest => dest.VatNumberStatusId, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.VatNumberStatusId, "")))
                .ForMember(dest => dest.ZipPostalCode, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.ZipPostalCode, "")))
                .ForMember(dest => dest.CountryId, mo => mo.MapFrom(src => src.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.CountryId, "")))
                .ForMember(dest => dest.Groups, mo => mo.MapFrom(src => src.Groups.Select(x => x)));

        }

        public int Order => 1;
    }
}
