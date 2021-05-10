using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Marketing.Services.Customers;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Common.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Customers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class CustomerReminderViewModelService : ICustomerReminderViewModelService
    {
        private readonly ICustomerService _customerService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ICustomerReminderService _customerReminderService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IServiceProvider _serviceProvider;

        #region Constructors

        public CustomerReminderViewModelService(ICustomerService customerService,
            ITranslationService translationService,
            ICustomerActivityService customerActivityService,
            ICustomerReminderService customerReminderService,
            IEmailAccountService emailAccountService,
            IDateTimeService dateTimeService,
            IServiceProvider serviceProvider)
        {
            _customerService = customerService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _customerReminderService = customerReminderService;
            _emailAccountService = emailAccountService;
            _dateTimeService = dateTimeService;
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Utilities
        #endregion
        public virtual CustomerReminderModel PrepareCustomerReminderModel()
        {
            var model = new CustomerReminderModel();
            model.StartDateTime = DateTime.Now;
            model.EndDateTime = DateTime.Now.AddMonths(1);
            model.LastUpdateDate = DateTime.Now.AddDays(-7);
            model.Active = true;
            return model;
        }

        public virtual async Task<CustomerReminder> InsertCustomerReminderModel(CustomerReminderModel model)
        {
            var customerreminder = model.ToEntity(_dateTimeService);
            await _customerReminderService.InsertCustomerReminder(customerreminder);
            //activity log
            await _customerActivityService.InsertActivity("AddNewCustomerReminder", customerreminder.Id, _translationService.GetResource("ActivityLog.AddNewCustomerReminder"), customerreminder.Name);
            return customerreminder;
        }
        public virtual async Task<CustomerReminder> UpdateCustomerReminderModel(CustomerReminder customerReminder, CustomerReminderModel model)
        {
            if (customerReminder.Conditions.Count() > 0)
                model.ReminderRuleId = customerReminder.ReminderRuleId;
            if (model.ReminderRuleId == 0)
                model.ReminderRuleId = customerReminder.ReminderRuleId;

            customerReminder = model.ToEntity(customerReminder, _dateTimeService);
            await _customerReminderService.UpdateCustomerReminder(customerReminder);
            await _customerActivityService.InsertActivity("EditCustomerReminder", customerReminder.Id, _translationService.GetResource("ActivityLog.EditCustomerReminder"), customerReminder.Name);
            return customerReminder;
        }
        public virtual async Task RunReminder(CustomerReminder customerReminder)
        {
            if (customerReminder.ReminderRuleId == CustomerReminderRuleEnum.AbandonedCart)
                await _customerReminderService.Task_AbandonedCart(customerReminder.Id);

            if (customerReminder.ReminderRuleId == CustomerReminderRuleEnum.Birthday)
                await _customerReminderService.Task_Birthday(customerReminder.Id);

            if (customerReminder.ReminderRuleId == CustomerReminderRuleEnum.LastActivity)
                await _customerReminderService.Task_LastActivity(customerReminder.Id);

            if (customerReminder.ReminderRuleId == CustomerReminderRuleEnum.LastPurchase)
                await _customerReminderService.Task_LastPurchase(customerReminder.Id);

            if (customerReminder.ReminderRuleId == CustomerReminderRuleEnum.RegisteredCustomer)
                await _customerReminderService.Task_RegisteredCustomer(customerReminder.Id);
        }
        public virtual async Task DeleteCustomerReminder(CustomerReminder customerReminder)
        {
            await _customerActivityService.InsertActivity("DeleteCustomerReminder", customerReminder.Id, _translationService.GetResource("ActivityLog.DeleteCustomerReminder"), customerReminder.Name);
            await _customerReminderService.DeleteCustomerReminder(customerReminder);

        }

        public virtual async Task<SerializeCustomerReminderHistoryModel> PrepareHistoryModelForList(SerializeCustomerReminderHistory history)
        {
            return new SerializeCustomerReminderHistoryModel
            {
                Id = history.Id,
                Email = (await _customerService.GetCustomerById(history.CustomerId)).Email,
                SendDate = _dateTimeService.ConvertToUserTime(history.SendDate, DateTimeKind.Utc),
                Level = history.Level,
                OrderId = !String.IsNullOrEmpty(history.OrderId)
            };
        }

        public virtual CustomerReminderModel.ConditionModel PrepareConditionModel(CustomerReminder customerReminder)
        {
            var model = new CustomerReminderModel.ConditionModel();
            model.CustomerReminderId = customerReminder.Id;
            foreach (CustomerReminderConditionTypeEnum item in Enum.GetValues(typeof(CustomerReminderConditionTypeEnum)))
            {
                if (customerReminder.ReminderRuleId == CustomerReminderRuleEnum.AbandonedCart || customerReminder.ReminderRuleId == CustomerReminderRuleEnum.CompletedOrder
                    || customerReminder.ReminderRuleId == CustomerReminderRuleEnum.UnpaidOrder)
                    model.ConditionType.Add(new SelectListItem()
                    {
                        Value = ((int)item).ToString(),
                        Text = item.ToString()
                    });
                else
                {
                    if (item != CustomerReminderConditionTypeEnum.Product &&
                        item != CustomerReminderConditionTypeEnum.Collection &&
                        item != CustomerReminderConditionTypeEnum.Category)
                    {
                        model.ConditionType.Add(new SelectListItem()
                        {
                            Value = ((int)item).ToString(),
                            Text = item.ToString()
                        });

                    }
                }
            }
            return model;
        }
        public virtual async Task<CustomerReminder.ReminderCondition> InsertConditionModel(CustomerReminder customerReminder, CustomerReminderModel.ConditionModel model)
        {
            var condition = new CustomerReminder.ReminderCondition()
            {
                Name = model.Name,
                ConditionTypeId = model.ConditionTypeId,
                ConditionId = model.ConditionId,
            };
            customerReminder.Conditions.Add(condition);
            await _customerReminderService.UpdateCustomerReminder(customerReminder);

            await _customerActivityService.InsertActivity("AddNewCustomerReminderCondition", customerReminder.Id, _translationService.GetResource("ActivityLog.AddNewCustomerReminder"), customerReminder.Name);

            return condition;
        }

        public virtual CustomerReminderModel.ConditionModel PrepareConditionModel(CustomerReminder customerReminder, CustomerReminder.ReminderCondition condition)
        {
            var model = condition.ToModel();
            model.CustomerReminderId = customerReminder.Id;
            foreach (CustomerReminderConditionTypeEnum item in Enum.GetValues(typeof(CustomerReminderConditionTypeEnum)))
            {
                model.ConditionType.Add(new SelectListItem()
                {
                    Value = ((int)item).ToString(),
                    Text = item.ToString()
                });
            }
            return model;
        }
        public virtual async Task<CustomerReminder.ReminderCondition> UpdateConditionModel(CustomerReminder customerReminder, CustomerReminder.ReminderCondition condition, CustomerReminderModel.ConditionModel model)
        {
            condition = model.ToEntity(condition);
            await _customerReminderService.UpdateCustomerReminder(customerReminder);
            await _customerActivityService.InsertActivity("EditCustomerReminderCondition", customerReminder.Id, _translationService.GetResource("ActivityLog.EditCustomerReminderCondition"), customerReminder.Name);
            return condition;
        }
        public virtual async Task ConditionDelete(string Id, string customerReminderId)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(customerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == Id);
                if (condition != null)
                {
                    customerReminder.Conditions.Remove(condition);
                    await _customerReminderService.UpdateCustomerReminder(customerReminder);
                }
            }
        }
        public virtual async Task ConditionDeletePosition(string id, string customerReminderId, string conditionId)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(customerReminderId);
            var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == conditionId);

            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.Product)
            {
                condition.Products.Remove(id);
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }
            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.Category)
            {
                condition.Categories.Remove(id);
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }
            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.Collection)
            {
                condition.Collections.Remove(id);
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }

            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerGroup)
            {
                condition.CustomerGroups.Remove(id);
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }
            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerTag)
            {
                condition.CustomerTags.Remove(id);
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }

            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomCustomerAttribute)
            {
                condition.CustomCustomerAttributes.Remove(condition.CustomCustomerAttributes.FirstOrDefault(x => x.Id == id));
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }
            if (condition.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerRegisterField)
            {
                condition.CustomerRegistration.Remove(condition.CustomerRegistration.FirstOrDefault(x => x.Id == id));
                await _customerReminderService.UpdateCustomerReminder(customerReminder);
            }
        }
        public virtual async Task InsertCategoryConditionModel(CustomerReminderModel.ConditionModel.AddCategoryConditionModel model)
        {
            foreach (string id in model.SelectedCategoryIds)
            {
                var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
                if (customerReminder != null)
                {
                    var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                    if (condition != null)
                    {
                        if (condition.Categories.Where(x => x == id).Count() == 0)
                        {
                            condition.Categories.Add(id);
                            await _customerReminderService.UpdateCustomerReminder(customerReminder);
                        }
                    }
                }
            }
        }
        public virtual async Task InsertCollectionConditionModel(CustomerReminderModel.ConditionModel.AddCollectionConditionModel model)
        {
            foreach (string id in model.SelectedCollectionIds)
            {
                var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
                if (customerReminder != null)
                {
                    var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                    if (condition != null)
                    {
                        if (condition.Collections.Where(x => x == id).Count() == 0)
                        {
                            condition.Collections.Add(id);
                            await _customerReminderService.UpdateCustomerReminder(customerReminder);
                        }
                    }
                }
            }
        }
        public virtual async Task InsertProductToConditionModel(CustomerReminderModel.ConditionModel.AddProductToConditionModel model)
        {
            foreach (string id in model.SelectedProductIds)
            {
                var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
                if (customerReminder != null)
                {
                    var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                    if (condition != null)
                    {
                        if (condition.Products.Where(x => x == id).Count() == 0)
                        {
                            condition.Products.Add(id);
                            await _customerReminderService.UpdateCustomerReminder(customerReminder);
                        }
                    }
                }
            }
        }
        public virtual async Task InsertCustomerTagConditionModel(CustomerReminderModel.ConditionModel.AddCustomerTagConditionModel model)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.CustomerTags.Where(x => x == model.CustomerTagId).Count() == 0)
                    {
                        condition.CustomerTags.Add(model.CustomerTagId);
                        await _customerReminderService.UpdateCustomerReminder(customerReminder);
                    }
                }
            }
        }
        public virtual async Task InsertCustomerGroupConditionModel(CustomerReminderModel.ConditionModel.AddCustomerGroupConditionModel model)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    if (condition.CustomerGroups.Where(x => x == model.CustomerGroupId).Count() == 0)
                    {
                        condition.CustomerGroups.Add(model.CustomerGroupId);
                        await _customerReminderService.UpdateCustomerReminder(customerReminder);
                    }
                }
            }
        }
        public virtual async Task InsertCustomerRegisterConditionModel(CustomerReminderModel.ConditionModel.AddCustomerRegisterConditionModel model)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _cr = new CustomerReminder.ReminderCondition.CustomerRegister()
                    {
                        RegisterField = model.CustomerRegisterName,
                        RegisterValue = model.CustomerRegisterValue,
                    };
                    condition.CustomerRegistration.Add(_cr);
                    await _customerReminderService.UpdateCustomerReminder(customerReminder);
                }
            }
        }
        public virtual async Task UpdateCustomerRegisterConditionModel(CustomerReminderModel.ConditionModel.AddCustomerRegisterConditionModel model)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var cr = condition.CustomerRegistration.FirstOrDefault(x => x.Id == model.Id);
                    cr.RegisterField = model.CustomerRegisterName;
                    cr.RegisterValue = model.CustomerRegisterValue;
                    await _customerReminderService.UpdateCustomerReminder(customerReminder);
                }
            }
        }
        public virtual async Task InsertCustomCustomerAttributeConditionModel(CustomerReminderModel.ConditionModel.AddCustomCustomerAttributeConditionModel model)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var _cr = new CustomerReminder.ReminderCondition.CustomerRegister()
                    {
                        RegisterField = model.CustomerAttributeName,
                        RegisterValue = model.CustomerAttributeValue,
                    };
                    condition.CustomCustomerAttributes.Add(_cr);
                    await _customerReminderService.UpdateCustomerReminder(customerReminder);
                }
            }
        }
        public virtual async Task UpdateCustomCustomerAttributeConditionModel(CustomerReminderModel.ConditionModel.AddCustomCustomerAttributeConditionModel model)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(model.CustomerReminderId);
            if (customerReminder != null)
            {
                var condition = customerReminder.Conditions.FirstOrDefault(x => x.Id == model.ConditionId);
                if (condition != null)
                {
                    var cr = condition.CustomCustomerAttributes.FirstOrDefault(x => x.Id == model.Id);
                    cr.RegisterField = model.CustomerAttributeName;
                    cr.RegisterValue = model.CustomerAttributeValue;
                    await _customerReminderService.UpdateCustomerReminder(customerReminder);
                }
            }
        }
        public virtual async Task PrepareReminderLevelModel(CustomerReminderModel.ReminderLevelModel model, CustomerReminder customerReminder)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var emailAccounts = await _emailAccountService.GetAllEmailAccounts();
            foreach (var item in emailAccounts)
            {
                model.EmailAccounts.Add(new SelectListItem
                {
                    Text = item.Email,
                    Value = item.Id.ToString()
                });
            }
            var messageTokenProvider = _serviceProvider.GetRequiredService<IMessageTokenProvider>();
            model.AllowedTokens = messageTokenProvider.GetListOfCustomerReminderAllowedTokens(customerReminder.ReminderRuleId);
        }
        public virtual async Task<CustomerReminder.ReminderLevel> InsertReminderLevel(CustomerReminder customerReminder, CustomerReminderModel.ReminderLevelModel model)
        {
            var level = new CustomerReminder.ReminderLevel()
            {
                Name = model.Name,
                Level = model.Level,
                BccEmailAddresses = model.BccEmailAddresses,
                Body = model.Body,
                EmailAccountId = model.EmailAccountId,
                Subject = model.Subject,
                Day = model.Day,
                Hour = model.Hour,
                Minutes = model.Minutes,
            };

            customerReminder.Levels.Add(level);
            await _customerReminderService.UpdateCustomerReminder(customerReminder);
            await _customerActivityService.InsertActivity("AddNewCustomerReminderLevel", customerReminder.Id, _translationService.GetResource("ActivityLog.AddNewCustomerReminderLevel"), customerReminder.Name);
            return level;
        }
        public virtual async Task<CustomerReminder.ReminderLevel> UpdateReminderLevel(CustomerReminder customerReminder, CustomerReminder.ReminderLevel customerReminderLevel, CustomerReminderModel.ReminderLevelModel model)
        {
            customerReminderLevel.Level = model.Level;
            customerReminderLevel.Name = model.Name;
            customerReminderLevel.Subject = model.Subject;
            customerReminderLevel.BccEmailAddresses = model.BccEmailAddresses;
            customerReminderLevel.Body = model.Body;
            customerReminderLevel.EmailAccountId = model.EmailAccountId;
            customerReminderLevel.Day = model.Day;
            customerReminderLevel.Hour = model.Hour;
            customerReminderLevel.Minutes = model.Minutes;
            await _customerReminderService.UpdateCustomerReminder(customerReminder);
            await _customerActivityService.InsertActivity("EditCustomerReminderCondition", customerReminder.Id, _translationService.GetResource("ActivityLog.EditCustomerReminderLevel"), customerReminder.Name);
            return customerReminderLevel;
        }
        public virtual async Task DeleteLevel(string Id, string customerReminderId)
        {
            var customerReminder = await _customerReminderService.GetCustomerReminderById(customerReminderId);
            if (customerReminder != null)
            {
                var level = customerReminder.Levels.FirstOrDefault(x => x.Id == Id);
                if (level != null)
                {
                    customerReminder.Levels.Remove(level);
                    await _customerReminderService.UpdateCustomerReminder(customerReminder);
                }
            }
        }
        public virtual async Task<(IList<ProductModel> products, int totalCount)> PrepareProductModel(CustomerActionConditionModel.AddProductToConditionModel model, int pageIndex, int pageSize)
        {
            var productService = _serviceProvider.GetRequiredService<IProductService>();
            var products = await productService.PrepareProductList(model.SearchCategoryId, model.SearchBrandId, model.SearchCollectionId, model.SearchStoreId, model.SearchVendorId, model.SearchProductTypeId, model.SearchProductName, pageIndex, pageSize);
            return (products.Select(x => x.ToModel(_dateTimeService)).ToList(), products.TotalCount);
        }
        public virtual async Task<CustomerReminderModel.ConditionModel.AddProductToConditionModel> PrepareProductToConditionModel(string customerReminderId, string conditionId)
        {
            var categoryService = _serviceProvider.GetRequiredService<ICategoryService>();
            var collectionService = _serviceProvider.GetRequiredService<ICollectionService>();
            var storeService = _serviceProvider.GetRequiredService<IStoreService>();
            var vendorService = _serviceProvider.GetRequiredService<IVendorService>();

            var model = new CustomerReminderModel.ConditionModel.AddProductToConditionModel();
            model.ConditionId = conditionId;
            model.CustomerReminderId = customerReminderId;
           
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList().ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

            return model;
        }
    }
}
