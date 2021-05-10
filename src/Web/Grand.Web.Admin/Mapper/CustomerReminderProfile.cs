using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerReminderProfile : Profile, IAutoMapperProfile
    {
        public CustomerReminderProfile()
        {
            CreateMap<CustomerReminderModel, CustomerReminder>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());

            CreateMap<CustomerReminder, CustomerReminderModel>();

            CreateMap<CustomerReminder.ReminderLevel, CustomerReminderModel.ReminderLevelModel>()
                .ForMember(dest => dest.EmailAccounts, mo => mo.Ignore());
            CreateMap<CustomerReminderModel.ReminderLevelModel, CustomerReminder.ReminderLevel>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());

            CreateMap<CustomerReminder.ReminderCondition, CustomerReminderModel.ConditionModel>()
                .ForMember(dest => dest.ConditionType, mo => mo.Ignore());
            CreateMap<CustomerReminderModel.ConditionModel, CustomerReminder.ReminderCondition>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}