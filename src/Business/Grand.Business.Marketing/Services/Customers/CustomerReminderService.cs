using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Customers
{
    public partial class CustomerReminderService : ICustomerReminderService
    {
        #region Fields

        private readonly IRepository<CustomerReminder> _customerReminderRepository;
        private readonly IRepository<CustomerReminderHistory> _customerReminderHistoryRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IMediator _mediator;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IStoreService _storeService;
        private readonly ICustomerAttributeParser _customerAttributeParser;
        private readonly IProductService _productService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly ILanguageService _languageService;

        #endregion

        #region Ctor

        public CustomerReminderService(
            IRepository<CustomerReminder> customerReminderRepository,
            IRepository<CustomerReminderHistory> customerReminderHistoryRepository,
            IRepository<Customer> customerRepository,
            IRepository<Order> orderRepository,
            IMediator mediator,
            IEmailAccountService emailAccountService,
            IQueuedEmailService queuedEmailService,
            IStoreService storeService,
            IProductService productService,
            ICustomerAttributeParser customerAttributeParser,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            ILanguageService languageService)
        {
            _customerReminderRepository = customerReminderRepository;
            _customerReminderHistoryRepository = customerReminderHistoryRepository;
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _mediator = mediator;
            _emailAccountService = emailAccountService;
            _queuedEmailService = queuedEmailService;
            _storeService = storeService;
            _customerAttributeParser = customerAttributeParser;
            _productService = productService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _languageService = languageService;
        }

        #endregion

        #region Utilities

        protected async Task<bool> SendEmail(Customer customer, CustomerReminder customerReminder, string reminderlevelId)
        {
            var reminderLevel = customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId);
            var emailAccount = await _emailAccountService.GetEmailAccountById(reminderLevel.EmailAccountId);
            var store = customer.ShoppingCartItems.Count > 0 ? await _storeService.GetStoreById(customer.ShoppingCartItems.FirstOrDefault().StoreId) : (await _storeService.GetAllStores()).FirstOrDefault();

            //retrieve message template data
            var bcc = reminderLevel.BccEmailAddresses;
            var languages = await _languageService.GetAllLanguages();
            var langId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, store?.Id);
            if (string.IsNullOrEmpty(langId))
                langId = languages.FirstOrDefault().Id;

            var language = languages.FirstOrDefault(x => x.Id == langId);
            if (language == null)
                language = languages.FirstOrDefault();

            var email = new QueuedEmail();

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddCustomerTokens(customer, store, language)
                   .AddShoppingCartTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            liquidObject.Email = new LiquidEmail(email.Id);

            var body = LiquidExtensions.Render(liquidObject, reminderLevel.Body);
            var subject = LiquidExtensions.Render(liquidObject, reminderLevel.Subject);

            //limit name length
            var toName = CommonHelper.EnsureMaximumLength(customer.GetFullName(), 300);

            email.PriorityId = QueuedEmailPriority.High;
            email.From = emailAccount.Email;
            email.FromName = emailAccount.DisplayName;
            email.To = customer.Email;
            email.ToName = toName;
            email.ReplyTo = string.Empty;
            email.ReplyToName = string.Empty;
            email.CC = string.Empty;
            email.Bcc = bcc;
            email.Subject = subject;
            email.Body = body;
            email.AttachmentFilePath = "";
            email.AttachmentFileName = "";
            email.AttachedDownloads = null;
            email.CreatedOnUtc = DateTime.UtcNow;
            email.EmailAccountId = emailAccount.Id;

            await _queuedEmailService.InsertQueuedEmail(email);
            //activity log
            await _customerActivityService.InsertActivity(string.Format("CustomerReminder.{0}", customerReminder.ReminderRuleId.ToString()), customer.Id, _translationService.GetResource(string.Format("ActivityLog.{0}", customerReminder.ReminderRuleId.ToString())), customer, customerReminder.Name);

            return true;
        }

        protected async Task<bool> SendEmail(Customer customer, Order order, CustomerReminder customerReminder, string reminderlevelId)
        {
            var reminderLevel = customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId);
            var emailAccount = await _emailAccountService.GetEmailAccountById(reminderLevel.EmailAccountId);
            var store = await _storeService.GetStoreById(customer.StoreId);
            if (order != null)
            {
                store = await _storeService.GetStoreById(order.StoreId);
            }
            if (store == null)
            {
                store = (await _storeService.GetAllStores()).FirstOrDefault();
            }

            //retrieve message template data
            var bcc = reminderLevel.BccEmailAddresses;
            Language language = null;
            if (order != null)
            {
                language = await _languageService.GetLanguageById(order.CustomerLanguageId);
            }
            else
            {
                var customerLanguageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
                if (!string.IsNullOrEmpty(customerLanguageId))
                    language = await _languageService.GetLanguageById(customerLanguageId);
            }
            if (language == null)
            {
                language = (await _languageService.GetAllLanguages()).FirstOrDefault();
            }

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddCustomerTokens(customer, store, language)
                   .AddShoppingCartTokens(customer, store, language)
                   .AddOrderTokens(order, customer, await _storeService.GetStoreById(order.StoreId));

            var liquidObject = await builder.BuildAsync();
            var body = LiquidExtensions.Render(liquidObject, reminderLevel.Body);
            var subject = LiquidExtensions.Render(liquidObject, reminderLevel.Subject);

            //limit name length
            var toName = CommonHelper.EnsureMaximumLength(customer.GetFullName(), 300);
            var email = new QueuedEmail
            {
                PriorityId = QueuedEmailPriority.High,
                From = emailAccount.Email,
                FromName = emailAccount.DisplayName,
                To = customer.Email,
                ToName = toName,
                ReplyTo = string.Empty,
                ReplyToName = string.Empty,
                CC = string.Empty,
                Bcc = bcc,
                Subject = subject,
                Body = body,
                AttachmentFilePath = "",
                AttachmentFileName = "",
                AttachedDownloads = null,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = emailAccount.Id,
            };

            await _queuedEmailService.InsertQueuedEmail(email);
            //activity log
            await _customerActivityService.InsertActivity(string.Format("CustomerReminder.{0}", customerReminder.ReminderRuleId.ToString()), customer.Id, string.Format("ActivityLog.{0}", customerReminder.ReminderRuleId.ToString()), customer, customerReminder.Name);

            return true;
        }


        #region Conditions
        protected async Task<bool> CheckConditions(CustomerReminder customerReminder, Customer customer)
        {
            if (customerReminder.Conditions.Count == 0)
                return true;


            bool cond = false;
            foreach (var item in customerReminder.Conditions)
            {
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.Category)
                {
                    cond = await ConditionCategory(item, customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart).Select(x => x.ProductId).ToList());
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.Product)
                {
                    cond = ConditionProducts(item, customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart).Select(x => x.ProductId).ToList());
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.Collection)
                {
                    cond = await ConditionCollection(item, customer.ShoppingCartItems.Where(x => x.ShoppingCartTypeId == ShoppingCartType.ShoppingCart).Select(x => x.ProductId).ToList());
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerTag)
                {
                    cond = ConditionCustomerTag(item, customer);
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerGroup)
                {
                    cond = ConditionCustomerGroup(item, customer);
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerRegisterField)
                {
                    cond = ConditionCustomerRegister(item, customer);
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomCustomerAttribute)
                {
                    cond = await ConditionCustomerAttribute(item, customer);
                }
            }

            return cond;
        }
        protected async Task<bool> CheckConditions(CustomerReminder customerReminder, Customer customer, Order order)
        {
            if (customerReminder.Conditions.Count == 0)
                return true;


            bool cond = false;
            foreach (var item in customerReminder.Conditions)
            {
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.Category)
                {
                    cond = await ConditionCategory(item, order.OrderItems.Select(x => x.ProductId).ToList());
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.Product)
                {
                    cond = ConditionProducts(item, order.OrderItems.Select(x => x.ProductId).ToList());
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.Collection)
                {
                    cond = await ConditionCollection(item, order.OrderItems.Select(x => x.ProductId).ToList());
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerTag)
                {
                    cond = ConditionCustomerTag(item, customer);
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerGroup)
                {
                    cond = ConditionCustomerGroup(item, customer);
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomerRegisterField)
                {
                    cond = ConditionCustomerRegister(item, customer);
                }
                if (item.ConditionTypeId == CustomerReminderConditionTypeEnum.CustomCustomerAttribute)
                {
                    cond = await ConditionCustomerAttribute(item, customer);
                }
            }

            return cond;
        }
        protected async Task<bool> ConditionCategory(CustomerReminder.ReminderCondition condition, ICollection<string> products)
        {
            bool cond = false;
            if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
            {
                cond = true;
                foreach (var item in condition.Categories)
                {
                    foreach (var product in products)
                    {
                        var pr = await _productService.GetProductById(product);
                        if (pr != null)
                        {
                            if (pr.ProductCategories.Where(x => x.CategoryId == item).Count() == 0)
                                return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
            {
                foreach (var item in condition.Categories)
                {
                    foreach (var product in products)
                    {
                        var pr = await _productService.GetProductById(product);
                        if (pr != null)
                        {
                            if (pr.ProductCategories.Where(x => x.CategoryId == item).Count() > 0)
                                return true;
                        }
                    }
                }
            }

            return cond;
        }
        protected async Task<bool> ConditionCollection(CustomerReminder.ReminderCondition condition, ICollection<string> products)
        {
            bool cond = false;
            if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
            {
                cond = true;
                foreach (var item in condition.Collections)
                {
                    foreach (var product in products)
                    {
                        var pr = await _productService.GetProductById(product);
                        if (pr != null)
                        {
                            if (pr.ProductCollections.Where(x => x.CollectionId == item).Count() == 0)
                                return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
            {
                foreach (var item in condition.Collections)
                {
                    foreach (var product in products)
                    {
                        var pr = await _productService.GetProductById(product);
                        if (pr != null)
                        {
                            if (pr.ProductCollections.Where(x => x.CollectionId == item).Count() > 0)
                                return true;
                        }
                    }
                }
            }

            return cond;
        }
        protected bool ConditionProducts(CustomerReminder.ReminderCondition condition, ICollection<string> products)
        {
            bool cond = true;
            if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
            {
                cond = products.ContainsAll(condition.Products);
            }
            if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
            {
                cond = products.ContainsAny(condition.Products);
            }

            return cond;
        }
        protected bool ConditionCustomerGroup(CustomerReminder.ReminderCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                var customerGroups = customer.Groups;
                if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
                {
                    cond = customerGroups.ContainsAll(condition.CustomerGroups);
                }
                if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
                {
                    cond = customerGroups.ContainsAny(condition.CustomerGroups);
                }
            }
            return cond;
        }
        protected bool ConditionCustomerTag(CustomerReminder.ReminderCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                var customerTags = customer.CustomerTags;
                if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
                {
                    cond = customerTags.Select(x => x).ContainsAll(condition.CustomerTags);
                }
                if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
                {
                    cond = customerTags.Select(x => x).ContainsAny(condition.CustomerTags);
                }
            }
            return cond;
        }
        protected bool ConditionCustomerRegister(CustomerReminder.ReminderCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
                {
                    cond = true;
                    foreach (var item in condition.CustomerRegistration)
                    {
                        if (customer.UserFields.Where(x => x.Key == item.RegisterField && x.Value == item.RegisterValue).Count() == 0)
                            cond = false;
                    }
                }
                if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
                {
                    foreach (var item in condition.CustomerRegistration)
                    {
                        if (customer.UserFields.Where(x => x.Key == item.RegisterField && x.Value == item.RegisterValue).Count() > 0)
                            cond = true;
                    }
                }
            }
            return cond;
        }
        protected async Task<bool> ConditionCustomerAttribute(CustomerReminder.ReminderCondition condition, Customer customer)
        {
            bool cond = false;
            if (customer != null)
            {
                if (condition.ConditionId == CustomerReminderConditionEnum.AllOfThem)
                {
                    if (customer.Attributes.Any())
                    {
                        var selectedValues = await _customerAttributeParser.ParseCustomerAttributeValues(customer.Attributes);
                        cond = true;
                        foreach (var item in condition.CustomCustomerAttributes)
                        {
                            var _fields = item.RegisterField.Split(':');
                            if (_fields.Count() > 1)
                            {
                                if (selectedValues.Where(x => x.CustomerAttributeId == _fields.FirstOrDefault() && x.Id == _fields.LastOrDefault()).Count() == 0)
                                    cond = false;
                            }
                            else
                                cond = false;
                        }
                    }
                }
                if (condition.ConditionId == CustomerReminderConditionEnum.OneOfThem)
                {
                    if (customer.Attributes.Any())
                    {
                        var selectedValues = await _customerAttributeParser.ParseCustomerAttributeValues(customer.Attributes);
                        foreach (var item in condition.CustomCustomerAttributes)
                        {
                            var _fields = item.RegisterField.Split(':');
                            if (_fields.Count() > 1)
                            {
                                if (selectedValues.Where(x => x.CustomerAttributeId == _fields.FirstOrDefault() && x.Id == _fields.LastOrDefault()).Count() > 0)
                                    cond = true;
                            }
                        }
                    }
                }
            }
            return cond;
        }
        #endregion

        #region History

        protected async Task UpdateHistory(Customer customer, CustomerReminder customerReminder, string reminderlevelId, CustomerReminderHistory history)
        {
            if (history != null)
            {
                history.Levels.Add(new CustomerReminderHistory.HistoryLevel()
                {
                    Level = customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId).Level,
                    ReminderLevelId = reminderlevelId,
                    SendDate = DateTime.UtcNow,
                });
                if (customerReminder.Levels.Max(x => x.Level) ==
                    customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId).Level)
                {
                    history.Status = CustomerReminderHistoryStatusEnum.CompletedReminder;
                    history.EndDate = DateTime.UtcNow;
                }
                await _customerReminderHistoryRepository.UpdateAsync(history);
            }
            else
            {
                history = new CustomerReminderHistory();
                history.CustomerId = customer.Id;
                history.Status = CustomerReminderHistoryStatusEnum.Started;
                history.StartDate = DateTime.UtcNow;
                history.CustomerReminderId = customerReminder.Id;
                history.ReminderRuleId = customerReminder.ReminderRuleId;
                history.Levels.Add(new CustomerReminderHistory.HistoryLevel()
                {
                    Level = customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId).Level,
                    ReminderLevelId = reminderlevelId,
                    SendDate = DateTime.UtcNow,
                });

                await _customerReminderHistoryRepository.InsertAsync(history);
            }

        }

        protected async Task UpdateHistory(Order order, CustomerReminder customerReminder, string reminderlevelId, CustomerReminderHistory history)
        {
            if (history != null)
            {
                history.Levels.Add(new CustomerReminderHistory.HistoryLevel()
                {
                    Level = customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId).Level,
                    ReminderLevelId = reminderlevelId,
                    SendDate = DateTime.UtcNow,
                });
                if (customerReminder.Levels.Max(x => x.Level) ==
                    customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId).Level)
                {
                    history.Status = CustomerReminderHistoryStatusEnum.CompletedReminder;
                    history.EndDate = DateTime.UtcNow;
                }
                await _customerReminderHistoryRepository.UpdateAsync(history);
            }
            else
            {
                history = new CustomerReminderHistory();
                history.BaseOrderId = order.Id;
                history.CustomerId = order.CustomerId;
                history.Status = CustomerReminderHistoryStatusEnum.Started;
                history.StartDate = DateTime.UtcNow;
                history.CustomerReminderId = customerReminder.Id;
                history.ReminderRuleId = customerReminder.ReminderRuleId;
                history.Levels.Add(new CustomerReminderHistory.HistoryLevel()
                {
                    Level = customerReminder.Levels.FirstOrDefault(x => x.Id == reminderlevelId).Level,
                    ReminderLevelId = reminderlevelId,
                    SendDate = DateTime.UtcNow,
                });

                await _customerReminderHistoryRepository.InsertAsync(history);
            }

        }
        protected async Task CloseHistoryReminder(CustomerReminder customerReminder, CustomerReminderHistory history)
        {
            history.Status = CustomerReminderHistoryStatusEnum.CompletedReminder;
            history.EndDate = DateTime.UtcNow;
            await _customerReminderHistoryRepository.UpdateAsync(history);
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Gets customer reminder
        /// </summary>
        /// <param name="id">Customer reminder identifier</param>
        /// <returns>Customer reminder</returns>
        public virtual Task<CustomerReminder> GetCustomerReminderById(string id)
        {
            return _customerReminderRepository.GetByIdAsync(id);
        }


        /// <summary>
        /// Gets all customer reminders
        /// </summary>
        /// <returns>Customer reminders</returns>
        public virtual async Task<IList<CustomerReminder>> GetCustomerReminders()
        {
            var query = from p in _customerReminderRepository.Table
                        orderby p.DisplayOrder
                        select p;
            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Inserts a customer reminder
        /// </summary>
        /// <param name="CustomerReminder">Customer reminder</param>
        public virtual async Task InsertCustomerReminder(CustomerReminder customerReminder)
        {
            if (customerReminder == null)
                throw new ArgumentNullException(nameof(customerReminder));

            await _customerReminderRepository.InsertAsync(customerReminder);

            //event notification
            await _mediator.EntityInserted(customerReminder);

        }

        /// <summary>
        /// Delete a customer reminder
        /// </summary>
        /// <param name="customerReminder">Customer reminder</param>
        public virtual async Task DeleteCustomerReminder(CustomerReminder customerReminder)
        {
            if (customerReminder == null)
                throw new ArgumentNullException(nameof(customerReminder));

            await _customerReminderRepository.DeleteAsync(customerReminder);

            //event notification
            await _mediator.EntityDeleted(customerReminder);

        }

        /// <summary>
        /// Updates the customer reminder
        /// </summary>
        /// <param name="CustomerReminder">Customer reminder</param>
        public virtual async Task UpdateCustomerReminder(CustomerReminder customerReminder)
        {
            if (customerReminder == null)
                throw new ArgumentNullException(nameof(customerReminder));

            await _customerReminderRepository.UpdateAsync(customerReminder);

            //event notification
            await _mediator.EntityUpdated(customerReminder);
        }



        public virtual async Task<IPagedList<SerializeCustomerReminderHistory>> GetAllCustomerReminderHistory(string customerReminderId, int pageIndex = 0, int pageSize = 2147483647)
        {
            var query = from h in _customerReminderHistoryRepository.Table
                        from l in h.Levels
                        select new SerializeCustomerReminderHistory() { CustomerId = h.CustomerId, Id = h.Id, CustomerReminderId = h.CustomerReminderId, Level = l.Level, SendDate = l.SendDate, OrderId = h.OrderId };

            query = from p in query
                    where p.CustomerReminderId == customerReminderId
                    select p;
            return await PagedList<SerializeCustomerReminderHistory>.Create(query, pageIndex, pageSize);
        }

        #endregion

        #region Tasks

        public virtual async Task Task_AbandonedCart(string id = "")
        {
            var datetimeUtcNow = DateTime.UtcNow.Date;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.AbandonedCart
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.AbandonedCart
                                          select cr).ToList();
            }

            foreach (var reminder in customerReminder)
            {
                var customers = (from cu in _customerRepository.Table
                                       where cu.ShoppingCartItems.Any() && cu.LastUpdateCartDateUtc > reminder.LastUpdateDate && cu.Active && !cu.Deleted
                                       && (!String.IsNullOrEmpty(cu.Email))
                                       select cu).ToList();

                foreach (var customer in customers)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.CustomerId == customer.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();
                    if (history.Any())
                    {
                        var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                        if (activereminderhistory != null)
                        {
                            var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                            var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                            if (reminderLevel != null)
                            {
                                if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                {
                                    var send = await SendEmail(customer, reminder, reminderLevel.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, reminderLevel.Id, activereminderhistory);
                                }
                            }
                            else
                            {
                                await CloseHistoryReminder(reminder, activereminderhistory);
                            }
                        }
                        else
                        {
                            if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                            {
                                var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                if (level != null)
                                {

                                    if (DateTime.UtcNow > customer.LastUpdateCartDateUtc.Value.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes))
                                    {
                                        if (await CheckConditions(reminder, customer))
                                        {
                                            var send = await SendEmail(customer, reminder, level.Id);
                                            if (send)
                                                await UpdateHistory(customer, reminder, level.Id, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                        if (level != null)
                        {

                            if (DateTime.UtcNow > customer.LastUpdateCartDateUtc.Value.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes))
                            {
                                if (await CheckConditions(reminder, customer))
                                {
                                    var send = await SendEmail(customer, reminder, level.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, level.Id, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task Task_RegisteredCustomer(string id = "")
        {
            var datetimeUtcNow = DateTime.UtcNow.Date;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.RegisteredCustomer
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.RegisteredCustomer
                                          select cr).ToList();
            }
            foreach (var reminder in customerReminder)
            {
                var customers = (from cu in _customerRepository.Table
                                       where cu.CreatedOnUtc > reminder.LastUpdateDate && cu.Active && !cu.Deleted
                                       && (!String.IsNullOrEmpty(cu.Email))
                                       && !cu.IsSystemAccount
                                       select cu).ToList();

                foreach (var customer in customers)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.CustomerId == customer.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();
                    if (history.Any())
                    {
                        var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                        if (activereminderhistory != null)
                        {
                            var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                            var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                            if (reminderLevel != null)
                            {
                                if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                {
                                    var send = await SendEmail(customer, reminder, reminderLevel.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, reminderLevel.Id, activereminderhistory);
                                }
                            }
                            else
                            {
                                await CloseHistoryReminder(reminder, activereminderhistory);
                            }
                        }
                        else
                        {
                            if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                            {
                                var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                if (level != null)
                                {

                                    if (DateTime.UtcNow > customer.CreatedOnUtc.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes))
                                    {
                                        if (await CheckConditions(reminder, customer))
                                        {
                                            var send = await SendEmail(customer, reminder, level.Id);
                                            if (send)
                                                await UpdateHistory(customer, reminder, level.Id, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                        if (level != null)
                        {

                            if (DateTime.UtcNow > customer.CreatedOnUtc.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes))
                            {
                                if (await CheckConditions(reminder, customer))
                                {
                                    var send = await SendEmail(customer, reminder, level.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, level.Id, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task Task_LastActivity(string id = "")
        {
            var datetimeUtcNow = DateTime.UtcNow.Date;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.LastActivity
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.LastActivity
                                          select cr).ToList();
            }
            foreach (var reminder in customerReminder)
            {
                var customers = (from cu in _customerRepository.Table
                                       where cu.LastActivityDateUtc < reminder.LastUpdateDate && cu.Active && !cu.Deleted
                                       && (!String.IsNullOrEmpty(cu.Email))
                                       select cu).ToList();

                foreach (var customer in customers)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.CustomerId == customer.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();
                    if (history.Any())
                    {
                        var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                        if (activereminderhistory != null)
                        {
                            var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                            var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                            if (reminderLevel != null)
                            {
                                if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                {
                                    var send = await SendEmail(customer, reminder, reminderLevel.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, reminderLevel.Id, activereminderhistory);
                                }
                            }
                            else
                            {
                                await CloseHistoryReminder(reminder, activereminderhistory);
                            }
                        }
                        else
                        {
                            if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                            {
                                var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                if (level != null)
                                {

                                    if (DateTime.UtcNow > customer.LastActivityDateUtc.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes))
                                    {
                                        if (await CheckConditions(reminder, customer))
                                        {
                                            var send = await SendEmail(customer, reminder, level.Id);
                                            if (send)
                                                await UpdateHistory(customer, reminder, level.Id, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                        if (level != null)
                        {
                            if (DateTime.UtcNow > customer.LastActivityDateUtc.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes))
                            {
                                if (await CheckConditions(reminder, customer))
                                {
                                    var send = await SendEmail(customer, reminder, level.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, level.Id, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task Task_LastPurchase(string id = "")
        {
            var datetimeUtcNow = DateTime.UtcNow.Date;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.LastPurchase
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.LastPurchase
                                          select cr).ToList();
            }
            foreach (var reminder in customerReminder)
            {
                var customers = (from cu in _customerRepository.Table
                                       where cu.LastPurchaseDateUtc < reminder.LastUpdateDate || cu.LastPurchaseDateUtc == null
                                       && (!String.IsNullOrEmpty(cu.Email)) && cu.Active && !cu.Deleted
                                       && !cu.IsSystemAccount
                                       select cu).ToList();

                foreach (var customer in customers)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.CustomerId == customer.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();
                    if (history.Any())
                    {
                        var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                        if (activereminderhistory != null)
                        {
                            var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                            var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                            if (reminderLevel != null)
                            {
                                if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                {
                                    var send = await SendEmail(customer, reminder, reminderLevel.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, reminderLevel.Id, activereminderhistory);
                                }
                            }
                            else
                            {
                                await CloseHistoryReminder(reminder, activereminderhistory);
                            }
                        }
                        else
                        {
                            if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                            {
                                var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                if (level != null)
                                {
                                    DateTime lastpurchaseDate = customer.LastPurchaseDateUtc.HasValue ? customer.LastPurchaseDateUtc.Value.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes) : DateTime.MinValue;
                                    if (DateTime.UtcNow > lastpurchaseDate)
                                    {
                                        if (await CheckConditions(reminder, customer))
                                        {
                                            var send = await SendEmail(customer, reminder, level.Id);
                                            if (send)
                                                await UpdateHistory(customer, reminder, level.Id, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                        if (level != null)
                        {
                            DateTime lastpurchaseDate = customer.LastPurchaseDateUtc.HasValue ? customer.LastPurchaseDateUtc.Value.AddDays(level.Day).AddHours(level.Hour).AddMinutes(level.Minutes) : DateTime.MinValue;
                            if (DateTime.UtcNow > lastpurchaseDate)
                            {
                                if (await CheckConditions(reminder, customer))
                                {
                                    var send = await SendEmail(customer, reminder, level.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, level.Id, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task Task_Birthday(string id = "")
        {
            var datetimeUtcNow = DateTime.UtcNow.Date;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.Birthday
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.Birthday
                                          select cr).ToList();
            }

            foreach (var reminder in customerReminder)
            {
                int day = 0;
                if (reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null)
                    day = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault().Day;

                string dateDDMM = DateTime.Now.AddDays(day).ToString("-MM-dd");

                var customers = (from cu in _customerRepository.Table
                                       where (!String.IsNullOrEmpty(cu.Email)) && cu.Active && !cu.Deleted
                                       && cu.UserFields.Any(x => x.Key == "DateOfBirth" && x.Value.Contains(dateDDMM))
                                       select cu).ToList();

                foreach (var customer in customers)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.CustomerId == customer.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();
                    if (history.Any())
                    {
                        var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                        if (activereminderhistory != null)
                        {
                            var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                            var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                            if (reminderLevel != null)
                            {
                                if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                {
                                    var send = await SendEmail(customer, reminder, reminderLevel.Id);
                                    if (send)
                                        await UpdateHistory(customer, reminder, reminderLevel.Id, activereminderhistory);
                                }
                            }
                            else
                            {
                                await CloseHistoryReminder(reminder, activereminderhistory);
                            }
                        }
                        else
                        {
                            if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                            {
                                var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                if (level != null)
                                {
                                    if (await CheckConditions(reminder, customer))
                                    {
                                        var send = await SendEmail(customer, reminder, level.Id);
                                        if (send)
                                            await UpdateHistory(customer, reminder, level.Id, null);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                        if (level != null)
                        {
                            if (await CheckConditions(reminder, customer))
                            {
                                var send = await SendEmail(customer, reminder, level.Id);
                                if (send)
                                    await UpdateHistory(customer, reminder, level.Id, null);
                            }
                        }
                    }
                }

                var activehistory = (from hc in _customerReminderHistoryRepository.Table
                                           where hc.CustomerReminderId == reminder.Id && hc.Status == CustomerReminderHistoryStatusEnum.Started
                                           select hc).ToList();

                foreach (var activereminderhistory in activehistory)
                {
                    var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                    var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                    var customer = _customerRepository.Table.FirstOrDefault(x => x.Id == activereminderhistory.CustomerId);
                    if (reminderLevel != null && customer != null && customer.Active && !customer.Deleted)
                    {
                        if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                        {
                            var send = await SendEmail(customer, reminder, reminderLevel.Id);
                            if (send)
                                await UpdateHistory(customer, reminder, reminderLevel.Id, activereminderhistory);
                        }
                    }
                    else
                    {
                        await CloseHistoryReminder(reminder, activereminderhistory);
                    }
                }
            }

        }

        public virtual async Task Task_CompletedOrder(string id = "")
        {
            var dateNow = DateTime.UtcNow.Date;
            var datetimeUtcNow = DateTime.UtcNow;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.CompletedOrder
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.CompletedOrder
                                          select cr).ToList();
            }

            foreach (var reminder in customerReminder)
            {
                int day = 0;
                if (reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null)
                    day = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault().Day;

                var orders = (from or in _orderRepository.Table
                                    where or.OrderStatusId == (int)OrderStatusSystem.Complete
                                    && or.CreatedOnUtc >= reminder.LastUpdateDate && or.CreatedOnUtc >= dateNow.AddDays(-day)
                                    select or).ToList();

                foreach (var order in orders)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.BaseOrderId == order.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();

                    Customer customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                    if (customer != null && customer.Active && !customer.Deleted)
                    {
                        if (history.Any())
                        {
                            var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                            if (activereminderhistory != null)
                            {
                                var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                                var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                                if (reminderLevel != null)
                                {
                                    if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                    {
                                        var send = await SendEmail(customer, order, reminder, reminderLevel.Id);
                                        if (send)
                                            await UpdateHistory(order, reminder, reminderLevel.Id, activereminderhistory);
                                    }
                                }
                                else
                                {
                                    await CloseHistoryReminder(reminder, activereminderhistory);
                                }
                            }
                            else
                            {
                                if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                                {
                                    var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                    if (level != null)
                                    {
                                        if (await CheckConditions(reminder, customer, order))
                                        {
                                            var send = await SendEmail(customer, order, reminder, level.Id);
                                            if (send)
                                                await UpdateHistory(order, reminder, level.Id, null);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                            if (level != null)
                            {
                                if (await CheckConditions(reminder, customer, order))
                                {
                                    var send = await SendEmail(customer, order, reminder, level.Id);
                                    if (send)
                                        await UpdateHistory(order, reminder, level.Id, null);
                                }
                            }
                        }
                    }
                }

                var activehistory = (from hc in _customerReminderHistoryRepository.Table
                                           where hc.CustomerReminderId == reminder.Id && hc.Status == CustomerReminderHistoryStatusEnum.Started
                                           select hc).ToList();

                foreach (var activereminderhistory in activehistory)
                {
                    var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                    var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                    var order = _orderRepository.Table.FirstOrDefault(x => x.Id == activereminderhistory.BaseOrderId);
                    var customer = _customerRepository.Table.FirstOrDefault(x => x.Id == order.CustomerId && x.Active && !x.Deleted);
                    if (reminderLevel != null && order != null && customer != null)
                    {
                        if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                        {
                            var send = await SendEmail(customer, order, reminder, reminderLevel.Id);
                            if (send)
                                await UpdateHistory(order, reminder, reminderLevel.Id, activereminderhistory);
                        }
                    }
                    else
                    {
                        await CloseHistoryReminder(reminder, activereminderhistory);
                    }
                }

            }

        }
        public virtual async Task Task_UnpaidOrder(string id = "")
        {
            var datetimeUtcNow = DateTime.UtcNow;
            var dateNow = DateTime.UtcNow.Date;
            var customerReminder = new List<CustomerReminder>();
            if (String.IsNullOrEmpty(id))
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Active && datetimeUtcNow >= cr.StartDateTimeUtc && datetimeUtcNow <= cr.EndDateTimeUtc
                                          && cr.ReminderRuleId == CustomerReminderRuleEnum.UnpaidOrder
                                          select cr).ToList();
            }
            else
            {
                customerReminder = (from cr in _customerReminderRepository.Table
                                          where cr.Id == id && cr.ReminderRuleId == CustomerReminderRuleEnum.UnpaidOrder
                                          select cr).ToList();
            }

            foreach (var reminder in customerReminder)
            {
                int day = 0;
                if (reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null)
                    day = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault().Day;

                var orders = (from or in _orderRepository.Table
                                    where or.PaymentStatusId == PaymentStatus.Pending
                                    && or.CreatedOnUtc >= reminder.LastUpdateDate && or.CreatedOnUtc >= dateNow.AddDays(-day)
                                    select or).ToList();

                foreach (var order in orders)
                {
                    var history = (from hc in _customerReminderHistoryRepository.Table
                                         where hc.BaseOrderId == order.Id && hc.CustomerReminderId == reminder.Id
                                         select hc).ToList();

                    Customer customer = await _customerRepository.GetByIdAsync(order.CustomerId);
                    if (customer != null && customer.Active && !customer.Deleted)
                    {
                        if (history.Any())
                        {
                            var activereminderhistory = history.FirstOrDefault(x => x.Status == CustomerReminderHistoryStatusEnum.Started);
                            if (activereminderhistory != null)
                            {
                                var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                                var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                                if (reminderLevel != null)
                                {
                                    if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                                    {
                                        var send = await SendEmail(customer, order, reminder, reminderLevel.Id);
                                        if (send)
                                            await UpdateHistory(order, reminder, reminderLevel.Id, activereminderhistory);
                                    }
                                }
                                else
                                {
                                    await CloseHistoryReminder(reminder, activereminderhistory);
                                }
                            }
                            else
                            {
                                if (DateTime.UtcNow > history.Max(x => x.EndDate).AddDays(reminder.RenewedDay) && reminder.AllowRenew)
                                {
                                    var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                                    if (level != null)
                                    {
                                        if (await CheckConditions(reminder, customer, order))
                                        {
                                            var send = await SendEmail(customer, order, reminder, level.Id);
                                            if (send)
                                                await UpdateHistory(order, reminder, level.Id, null);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var level = reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() != null ? reminder.Levels.OrderBy(x => x.Level).FirstOrDefault() : null;
                            if (level != null)
                            {
                                if (await CheckConditions(reminder, customer, order))
                                {
                                    var send = await SendEmail(customer, order, reminder, level.Id);
                                    if (send)
                                        await UpdateHistory(order, reminder, level.Id, null);
                                }
                            }
                        }
                    }
                }
                var activehistory = (from hc in _customerReminderHistoryRepository.Table
                                           where hc.CustomerReminderId == reminder.Id && hc.Status == CustomerReminderHistoryStatusEnum.Started
                                           select hc).ToList();

                foreach (var activereminderhistory in activehistory)
                {
                    var lastLevel = activereminderhistory.Levels.OrderBy(x => x.SendDate).LastOrDefault();
                    var reminderLevel = reminder.Levels.FirstOrDefault(x => x.Level > lastLevel.Level);
                    var order = _orderRepository.Table.FirstOrDefault(x => x.Id == activereminderhistory.BaseOrderId);
                    var customer = _customerRepository.Table.FirstOrDefault(x => x.Id == order.CustomerId && x.Active && !x.Deleted);
                    if (reminderLevel != null && order != null && customer != null)
                    {
                        if (order.PaymentStatusId == PaymentStatus.Pending)
                        {
                            if (DateTime.UtcNow > lastLevel.SendDate.AddDays(reminderLevel.Day).AddHours(reminderLevel.Hour).AddMinutes(reminderLevel.Minutes))
                            {
                                var send = await SendEmail(customer, order, reminder, reminderLevel.Id);
                                if (send)
                                    await UpdateHistory(order, reminder, reminderLevel.Id, activereminderhistory);
                            }
                        }
                        else
                            await CloseHistoryReminder(reminder, activereminderhistory);

                    }
                    else
                    {
                        await CloseHistoryReminder(reminder, activereminderhistory);
                    }
                }
            }
        }

        #endregion
    }

    public class SerializeCustomerReminderHistory
    {
        public string Id { get; set; }
        public string CustomerReminderId { get; set; }
        public string CustomerId { get; set; }
        public DateTime SendDate { get; set; }
        public int Level { get; set; }
        public string OrderId { get; set; }
    }
}