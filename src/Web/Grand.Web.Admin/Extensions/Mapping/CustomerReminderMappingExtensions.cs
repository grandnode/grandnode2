using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Customers;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Extensions
{
    public static class CustomerReminderMappingExtensions
    {
        public static CustomerReminderModel ToModel(this CustomerReminder entity, IDateTimeService dateTimeService)
        {
            var reminder = entity.MapTo<CustomerReminder, CustomerReminderModel>();
            reminder.StartDateTime = entity.StartDateTimeUtc.ConvertToUserTime(dateTimeService);
            reminder.EndDateTime = entity.EndDateTimeUtc.ConvertToUserTime(dateTimeService);
            reminder.LastUpdateDate = entity.LastUpdateDate.ConvertToUserTime(dateTimeService);
            return reminder;

        }

        public static CustomerReminder ToEntity(this CustomerReminderModel model, IDateTimeService dateTimeService)
        {
            var reminder = model.MapTo<CustomerReminderModel, CustomerReminder>();
            reminder.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
            reminder.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
            reminder.LastUpdateDate = model.LastUpdateDate.ConvertToUtcTime(dateTimeService);
            return reminder;

        }

        public static CustomerReminder ToEntity(this CustomerReminderModel model, CustomerReminder destination, IDateTimeService dateTimeService)
        {
            var reminder = model.MapTo(destination);
            reminder.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
            reminder.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
            reminder.LastUpdateDate = model.LastUpdateDate.ConvertToUtcTime(dateTimeService);
            return reminder;
        }

        public static CustomerReminderModel.ReminderLevelModel ToModel(this CustomerReminder.ReminderLevel entity)
        {
            return entity.MapTo<CustomerReminder.ReminderLevel, CustomerReminderModel.ReminderLevelModel>();
        }

        public static CustomerReminder.ReminderLevel ToEntity(this CustomerReminderModel.ReminderLevelModel model)
        {
            return model.MapTo<CustomerReminderModel.ReminderLevelModel, CustomerReminder.ReminderLevel>();
        }

        public static CustomerReminder.ReminderLevel ToEntity(this CustomerReminderModel.ReminderLevelModel model, CustomerReminder.ReminderLevel destination)
        {
            return model.MapTo(destination);
        }

        public static CustomerReminderModel.ConditionModel ToModel(this CustomerReminder.ReminderCondition entity)
        {
            return entity.MapTo<CustomerReminder.ReminderCondition, CustomerReminderModel.ConditionModel>();
        }

        public static CustomerReminder.ReminderCondition ToEntity(this CustomerReminderModel.ConditionModel model)
        {
            return model.MapTo<CustomerReminderModel.ConditionModel, CustomerReminder.ReminderCondition>();
        }

        public static CustomerReminder.ReminderCondition ToEntity(this CustomerReminderModel.ConditionModel model, CustomerReminder.ReminderCondition destination)
        {
            return model.MapTo(destination);
        }
    }
}