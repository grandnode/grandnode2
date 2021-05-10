using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Customers;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Extensions
{
    public static class CustomerActionMappingExtensions
    {
        public static CustomerActionModel ToModel(this CustomerAction entity, IDateTimeService dateTimeService)
        {
            var action = entity.MapTo<CustomerAction, CustomerActionModel>();
            action.StartDateTime = entity.StartDateTimeUtc.ConvertToUserTime(dateTimeService);
            action.EndDateTime = entity.EndDateTimeUtc.ConvertToUserTime(dateTimeService);
            return action;
        }

        public static CustomerAction ToEntity(this CustomerActionModel model, IDateTimeService dateTimeService)
        {
            var action = model.MapTo<CustomerActionModel, CustomerAction>();
            action.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
            action.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
            return action;
        }

        public static CustomerAction ToEntity(this CustomerActionModel model, CustomerAction destination, IDateTimeService dateTimeService)
        {
            var action = model.MapTo(destination);
            action.StartDateTimeUtc = model.StartDateTime.ConvertToUtcTime(dateTimeService);
            action.EndDateTimeUtc = model.EndDateTime.ConvertToUtcTime(dateTimeService);
            return action;
        }

        public static CustomerActionConditionModel ToModel(this CustomerAction.ActionCondition entity)
        {
            return entity.MapTo<CustomerAction.ActionCondition, CustomerActionConditionModel>();
        }

        public static CustomerAction.ActionCondition ToEntity(this CustomerActionConditionModel model)
        {
            return model.MapTo<CustomerActionConditionModel, CustomerAction.ActionCondition>();
        }

        public static CustomerAction.ActionCondition ToEntity(this CustomerActionConditionModel model, CustomerAction.ActionCondition destination)
        {
            return model.MapTo(destination);
        }
    }
}