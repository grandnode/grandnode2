using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Business.Messages.Extensions;
using Grand.Business.Messages.Interfaces;
using Grand.Business.Messages.Queries.Models;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Services
{
    public partial class MessageProviderService : IMessageProviderService
    {
        #region Fields

        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ILanguageService _languageService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IStoreService _storeService;
        private readonly IGroupService _groupService;
        private readonly IMediator _mediator;

        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly CommonSettings _commonSettings;

        #endregion

        #region Ctor

        public MessageProviderService(IMessageTemplateService messageTemplateService,
            IQueuedEmailService queuedEmailService,
            ILanguageService languageService,
            IEmailAccountService emailAccountService,
            IMessageTokenProvider messageTokenProvider,
            IStoreService storeService,
            IGroupService groupService,
            IMediator mediator,
            EmailAccountSettings emailAccountSettings,
            CommonSettings commonSettings)
        {
            _messageTemplateService = messageTemplateService;
            _queuedEmailService = queuedEmailService;
            _languageService = languageService;
            _emailAccountService = emailAccountService;
            _messageTokenProvider = messageTokenProvider;
            _storeService = storeService;
            _groupService = groupService;
            _emailAccountSettings = emailAccountSettings;
            _commonSettings = commonSettings;
            _mediator = mediator;
        }

        #endregion

        #region Utilities

        protected virtual async Task<Store> GetStore(string storeId)
        {
            return await _storeService.GetStoreById(storeId) ?? (await _storeService.GetAllStores()).FirstOrDefault();
        }

        protected virtual async Task<MessageTemplate> GetMessageTemplate(string messageTemplateName, string storeId)
        {
            var messageTemplate = await _messageTemplateService.GetMessageTemplateByName(messageTemplateName, storeId);

            //no template found
            if (messageTemplate == null)
                return null;

            //ensure it's active
            var isActive = messageTemplate.IsActive;
            if (!isActive)
                return null;

            return messageTemplate;
        }

        protected virtual async Task<EmailAccount> GetEmailAccountOfMessageTemplate(MessageTemplate messageTemplate, string languageId)
        {
            var emailAccounId = messageTemplate.GetTranslation(mt => mt.EmailAccountId, languageId);
            var emailAccount = await _emailAccountService.GetEmailAccountById(emailAccounId);
            if (emailAccount == null)
                emailAccount = await _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount == null)
                emailAccount = (await _emailAccountService.GetAllEmailAccounts()).FirstOrDefault();
            return emailAccount;

        }

        protected virtual async Task<Language> EnsureLanguageIsActive(string languageId, string storeId)
        {
            //load language by specified ID
            var language = await _languageService.GetLanguageById(languageId);

            if (language == null || !language.Published)
            {
                //load any language from the specified store
                language = (await _languageService.GetAllLanguages(storeId: storeId)).FirstOrDefault();
            }
            if (language == null || !language.Published)
            {
                //load any language
                language = (await _languageService.GetAllLanguages()).FirstOrDefault();
            }

            if (language == null)
                throw new Exception("No active language could be loaded");
            return language;
        }

        #endregion

        #region Methods

        #region Customer messages

        /// <summary>
        /// Send a message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="templateName">Message template name</param>
        /// <param name="toEmailAccount">Send email to email account</param>
        /// <param name="customerNote">Customer note</param>
        /// <returns>Queued email identifier</returns>
        protected virtual async Task<int> SendCustomerMessage(Customer customer, Store store, string languageId, string templateName, bool toEmailAccount = false, CustomerNote customerNote = null)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(templateName, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddCustomerTokens(customer, store, language, customerNote);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = toEmailAccount ? emailAccount.Email : customer.Email;
            var toName = toEmailAccount ? emailAccount.DisplayName : customer.GetFullName();
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends 'New customer' notification message to a store owner
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store identifier</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerRegisteredMessage(Customer customer, Store store, string languageId)
        {
            return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerRegistered, true);
        }

        /// <summary>
        /// Sends a welcome message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerWelcomeMessage(Customer customer, Store store, string languageId)
        {
            return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerWelcome);
        }

        /// <summary>
        /// Sends an email validation message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerEmailValidationMessage(Customer customer, Store store, string languageId)
        {
            return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerEmailValidation);
        }

        /// <summary>
        /// Sends password recovery message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerPasswordRecoveryMessage(Customer customer, Store store, string languageId)
        {
            return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerPasswordRecovery);
        }

        /// <summary>
        /// Sends a new customer note added notification to a customer
        /// </summary>
        /// <param name="customerNote">Customer note</param>
        /// <param name="customer">Customer</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewCustomerNoteMessage(CustomerNote customerNote, Customer customer, Store store, string languageId)
        {
            return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerNewCustomerNote, customerNote: customerNote);
        }

        /// <summary>
        /// Send an email token validation message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store instance</param>
        /// <param name="token">Token</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerEmailTokenValidationMessage(Customer customer, Store store, string languageId)
        {
            return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerEmailTokenValidationMessage);
        }

        #endregion

        #region Order messages

        /// <summary>
        /// Sends an order placed notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderPlacedStoreOwnerMessage(Order order, Customer customer, string languageId)
        {
            return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderPlacedStoreOwnerMessage, order, customer, languageId);
        }

        /// <summary>
        /// Sends an order paid notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderPaidStoreOwnerMessage(Order order, Customer customer, string languageId)
        {
            return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderPaidStoreOwnerMessage, order, customer, languageId);
        }

        /// <summary>
        /// Sends an order cancelled notification to an admin
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderCancelledStoreOwnerMessage(Order order, Customer customer, string languageId)
        {
            return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderCancelledStoreOwnerMessage, order, customer, languageId);
        }

        /// Sends an order refunded notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="refundedAmount">Amount refunded</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderRefundedStoreOwnerMessage(Order order, double refundedAmount, string languageId)
        {
            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = order.CustomerId });
            return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderRefundedStoreOwnerMessage, order, customer, languageId, refundedAmount);
        }
        /// <summary>
        /// Sends an order notification to a store owner
        /// </summary>
        /// <param name="template">Message template</param>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        private async Task<int> SendOrderStoreOwnerMessage(string template, Order order, Customer customer, string languageId, double refundedAmount = 0)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(template, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var liquidBuilder = new LiquidObjectBuilder(_mediator);
            liquidBuilder.AddStoreTokens(store, language, emailAccount)
                         .AddOrderTokens(order, customer, store);
            if (customer != null)
                liquidBuilder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await liquidBuilder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Sends an order placed notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer"></param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderPlacedCustomerMessage(Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null)
        {
            return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderPlacedCustomerMessage, order, customer, languageId, attachmentFilePath, attachmentFileName, attachments);
        }

        /// <summary>
        /// Sends an order paid notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments ident</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderPaidCustomerMessage(Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null)
        {
            return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderPaidCustomerMessage, order, customer, languageId, attachmentFilePath, attachmentFileName, attachments);
        }

        /// <summary>
        /// Sends an order completed notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments ident</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderCompletedCustomerMessage(Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null)
        {
            return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderCompletedCustomerMessage, order, customer, languageId, attachmentFilePath, attachmentFileName, attachments);
        }

        /// <summary>
        /// Sends an order cancelled notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderCancelledCustomerMessage(Order order, Customer customer, string languageId)
        {
            return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderCancelledCustomerMessage, order, customer, languageId);
        }

        /// <summary>
        /// Sends an order refunded notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="refundedAmount">Amount refunded</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderRefundedCustomerMessage(Order order, double refundedAmount, string languageId)
        {
            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = order.CustomerId });
            return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderRefundedCustomerMessage, order, customer, languageId, refundedAmount: refundedAmount);
        }

        /// <summary>
        /// Sends an order notification to a customer
        /// </summary>
        /// <param name="message">Message template</param>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments ident</param>
        /// <returns>Queued email identifier</returns>
        private async Task<int> SendOrderCustomerMessage(string message, Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null, double refundedAmount = 0)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(message, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddOrderTokens(order, customer, store);

            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName,
                attachmentFilePath,
                attachmentFileName,
                attachments);
        }


        /// <summary>
        /// Sends an order placed notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderPlacedVendorMessage(Order order, Customer customer, Vendor vendor, string languageId)
        {
            return await SendOrderVendorMessage(MessageTemplateNames.SendOrderPlacedVendorMessage, order, vendor, languageId);
        }

        /// <summary>
        /// Sends an order paid notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderPaidVendorMessage(Order order, Vendor vendor, string languageId)
        {
            return await SendOrderVendorMessage(MessageTemplateNames.SendOrderPaidVendorMessage, order, vendor, languageId);
        }
        /// <summary>
        /// Sends an order cancel notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOrderCancelledVendorMessage(Order order, Vendor vendor, string languageId)
        {
            return await SendOrderVendorMessage(MessageTemplateNames.SendOrderCancelledVendorMessage, order, vendor, languageId);
        }
        /// <summary>
        /// Sends an order notification to a vendor
        /// </summary>
        /// <param name="message">Message template</param>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        private async Task<int> SendOrderVendorMessage(string message, Order order, Vendor vendor, string languageId)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(message, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = order.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddOrderTokens(order, customer, store, vendor: vendor);

            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = vendor.Email;
            var toName = vendor.Name;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Sends a shipment sent notification to a customer
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendShipmentSentCustomerMessage(Shipment shipment, Order order)
        {
            return await SendShipmentCustomerMessage(MessageTemplateNames.SendShipmentSentCustomerMessage, shipment, order);
        }

        /// <summary>
        /// Sends a shipment delivered notification to a customer
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendShipmentDeliveredCustomerMessage(Shipment shipment, Order order)
        {
            return await SendShipmentCustomerMessage(MessageTemplateNames.SendShipmentDeliveredCustomerMessage, shipment, order);
        }

        /// <summary>
        /// Send a shipment notification to a customer
        /// </summary>
        /// <param name="message">Message template</param>
        /// <param name="shipment">Shipment</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        private async Task<int> SendShipmentCustomerMessage(string message, Shipment shipment, Order order)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            if (order == null)
                throw new Exception("Order cannot be loaded");

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(order.CustomerLanguageId, store.Id);

            var messageTemplate = await GetMessageTemplate(message, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = order.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddShipmentTokens(shipment, order, store, language)
                   .AddOrderTokens(order, customer, store);

            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return await SendNotification(messageTemplate, emailAccount,
                language.Id, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Sends a new order note added notification to a customer
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="orderNote">Order note</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewOrderNoteAddedCustomerMessage(Order order, OrderNote orderNote)
        {
            if (orderNote == null)
                throw new ArgumentNullException(nameof(orderNote));

            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(order.CustomerLanguageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendNewOrderNoteAddedCustomerMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = order.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddOrderTokens(order, customer, store, orderNote);
            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return await SendNotification(messageTemplate, emailAccount,
                language.Id, liquidObject,
                toEmail, toName);
        }

        #endregion

        #region Newsletter messages

        /// <summary>
        /// Sends a newsletter subscription activation message
        /// </summary>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewsLetterSubscriptionActivationMessage(NewsLetterSubscription subscription,
            string languageId)
        {
            return await SendNewsLetterSubscriptionMessage(MessageTemplateNames.SendNewsLetterSubscriptionActivationMessage, subscription, languageId);
        }

        /// <summary>
        /// Sends a newsletter subscription deactivation message
        /// </summary>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewsLetterSubscriptionDeactivationMessage(NewsLetterSubscription subscription,
            string languageId)
        {
            return await SendNewsLetterSubscriptionMessage(MessageTemplateNames.SendNewsLetterSubscriptionDeactivationMessage, subscription, languageId);
        }

        /// <summary>
        /// Send a newsletter subscription message
        /// </summary>
        /// <param name="message">Message template</param>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        private async Task<int> SendNewsLetterSubscriptionMessage(string message, NewsLetterSubscription subscription,
            string languageId)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            var store = await GetStore(subscription.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(message, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddNewsLetterSubscriptionTokens(subscription, store);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = subscription.Email;
            var toName = "";
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }
        #endregion

        #region Send a message to a friend, ask question

        /// <summary>
        /// Sends "email a friend" message
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product instance</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendProductEmailAFriendMessage(Customer customer, Store store, string languageId,
            Product product, string customerEmail, string friendsEmail, string personalMessage)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendProductEmailAFriendMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddEmailAFriendTokens(personalMessage, customerEmail, friendsEmail)
                   .AddCustomerTokens(customer, store, language)
                   .AddProductTokens(product, language, store);
            LiquidObject liquidObject = await builder.BuildAsync();

            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = friendsEmail;
            var toName = "";
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends wishlist "email a friend" message
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendWishlistEmailAFriendMessage(Customer customer, Store store, string languageId,
             string customerEmail, string friendsEmail, string personalMessage)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendWishlistEmailAFriendMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);


            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                    .AddCustomerTokens(customer, store, language)
                    .AddEmailAFriendTokens(personalMessage, customerEmail, friendsEmail);

            LiquidObject liquidObject = await builder.BuildAsync();

            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = friendsEmail;
            var toName = "";
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Sends "email a friend" message
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product instance</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendProductQuestionMessage(Customer customer, Store store, string languageId,
            Product product, string customerEmail, string fullName, string phone, string message)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendProductQuestionMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                    .AddCustomerTokens(customer, store, language)
                    .AddProductTokens(product, language, store);
            LiquidObject liquidObject = await builder.BuildAsync();
            liquidObject.AskQuestion = new LiquidAskQuestion(message, customerEmail, fullName, phone);

            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            //store in database
            if (_commonSettings.StoreInDatabaseContactUsForm)
            {
                var subject = messageTemplate.GetTranslation(mt => mt.Subject, languageId);
                var body = messageTemplate.GetTranslation(mt => mt.Body, languageId);

                var subjectReplaced = LiquidExtensions.Render(liquidObject, subject);
                var bodyReplaced = LiquidExtensions.Render(liquidObject, body);

                await _mediator.Send(new InsertContactUsCommand()
                {
                    CustomerId = customer.Id,
                    StoreId = store.Id,
                    VendorId = product.VendorId,
                    Email = customerEmail,
                    Enquiry = bodyReplaced,
                    FullName = fullName,
                    Subject = subjectReplaced,
                    EmailAccountId = emailAccount.Id
                });
            }

            var toEmail = emailAccount.Email;
            var toName = "";

            if (!string.IsNullOrEmpty(product?.VendorId))
            {
                var vendor = await _mediator.Send(new GetVendorByIdQuery() { Id = product.VendorId });
                if (vendor != null)
                {
                    toEmail = vendor.Email;
                    toName = vendor.Name;
                }
            }
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName, replyToEmailAddress: customerEmail);
        }

        #endregion

        #region Merchandise returns

        /// <summary>
        /// Sends 'New Merchandise Return' message to a store owner
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="orderItem">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewMerchandiseReturnStoreOwnerMessage(MerchandiseReturn merchandiseReturn, Order order, string languageId)
        {
            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendNewMerchandiseReturnStoreOwnerMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = merchandiseReturn.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);

            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            builder.AddMerchandiseReturnTokens(merchandiseReturn, store, order, language);

            LiquidObject liquidObject = await builder.BuildAsync();

            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            if (!string.IsNullOrEmpty(merchandiseReturn.VendorId))
            {
                var vendor = await _mediator.Send(new GetVendorByIdQuery() { Id = merchandiseReturn.VendorId });
                if (vendor != null)
                {
                    var vendorEmail = vendor.Email;
                    var vendorName = vendor.Name;
                    await SendNotification(messageTemplate, emailAccount,
                        languageId, liquidObject,
                        vendorEmail, vendorName);
                }
            }
            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends 'Merchandise Return status changed' message to a customer
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendMerchandiseReturnStatusChangedCustomerMessage(MerchandiseReturn merchandiseReturn, Order order, string languageId)
        {
            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendMerchandiseReturnStatusChangedCustomerMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = merchandiseReturn.CustomerId });


            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);
            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            builder.AddMerchandiseReturnTokens(merchandiseReturn, store, order, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            string toEmail = string.IsNullOrEmpty(customer.Email) ?
                order.BillingAddress.Email :
                customer.Email;
            var toName = string.IsNullOrEmpty(customer.Email) ?
                order.BillingAddress.FirstName :
                customer.GetFullName();
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends 'New Merchandise Return' message to a customer
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewMerchandiseReturnCustomerMessage(MerchandiseReturn merchandiseReturn, Order order, string languageId)
        {
            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendNewMerchandiseReturnCustomerMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = merchandiseReturn.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);
            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            builder.AddMerchandiseReturnTokens(merchandiseReturn, store, order, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = string.IsNullOrEmpty(customer.Email) ?
                order.BillingAddress.Email :
                customer.Email;
            var toName = string.IsNullOrEmpty(customer.Email) ?
                order.BillingAddress.FirstName :
                customer.GetFullName();
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a new merchandise return note added notification to a customer
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="merchandiseReturnNote">Merchandise return note</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewMerchandiseReturnNoteAddedCustomerMessage(MerchandiseReturn merchandiseReturn, MerchandiseReturnNote merchandiseReturnNote, Order order)
        {
            if (merchandiseReturnNote == null)
                throw new ArgumentNullException(nameof(merchandiseReturnNote));

            if (merchandiseReturn == null)
                throw new ArgumentNullException(nameof(merchandiseReturn));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(order.CustomerLanguageId, store.Id);

            var messageTemplate = await GetMessageTemplate(MessageTemplateNames.SendNewMerchandiseReturnNoteAddedCustomerMessage, store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = merchandiseReturn.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);
            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            builder.AddMerchandiseReturnTokens(merchandiseReturn, store, order, language, merchandiseReturnNote);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = string.IsNullOrEmpty(customer.Email) ?
                order.BillingAddress.Email :
                customer.Email;
            var toName = string.IsNullOrEmpty(customer.Email) ?
                order.BillingAddress.FirstName :
                customer.GetFullName();
            return await SendNotification(messageTemplate, emailAccount,
                language.Id, liquidObject,
                toEmail, toName);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sends 'New vendor account submitted' message to a store owner
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="vendor">Vendor</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewVendorAccountApplyStoreOwnerMessage(Customer customer, Vendor vendor, Store store, string languageId)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("VendorAccountApply.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator).AddStoreTokens(store, language, emailAccount)
                                                     .AddCustomerTokens(customer, store, language)
                                                     .AddVendorTokens(vendor, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends 'Vendor information changed' message to a store owner
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendVendorInformationChangeMessage(Vendor vendor, Store store, string languageId)
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("VendorInformationChange.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddVendorTokens(vendor, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;

            return await SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName);
        }


        /// <summary>
        /// Sends a gift voucher notification
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        /// <param name="order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendGiftVoucherMessage(GiftVoucher giftVoucher, Order order, string languageId)
        {
            if (giftVoucher == null)
                throw new ArgumentNullException(nameof(giftVoucher));

            Store store = null;
            if (order != null)
                store = await _storeService.GetStoreById(order.StoreId);
            if (store == null)
                store = (await _storeService.GetAllStores()).FirstOrDefault();

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("GiftVoucher.Notification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                    .AddGiftVoucherTokens(giftVoucher, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);
            var toEmail = giftVoucher.RecipientEmail;
            var toName = giftVoucher.RecipientName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a product review notification message to a store owner
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productReview">Product review</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendProductReviewMessage(Product product, ProductReview productReview,
            Store store, string languageId)
        {
            if (productReview == null)
                throw new ArgumentNullException(nameof(productReview));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("Product.ProductReview", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = productReview.CustomerId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddProductReviewTokens(product, productReview);

            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Sends a vendor review notification message to a store owner
        /// </summary>
        /// <param name="vendorReview">Vendor review</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendVendorReviewMessage(VendorReview vendorReview, Store store,
            string languageId)
        {
            if (vendorReview == null)
                throw new ArgumentNullException(nameof(vendorReview));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("Vendor.VendorReview", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);
            //customer
            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = vendorReview.CustomerId });
            //vendor
            var vendor = await _mediator.Send(new GetVendorByIdQuery() { Id = vendorReview.VendorId });

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                    .AddVendorReviewTokens(vendor, vendorReview);

            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            builder.AddVendorTokens(vendor, language);
            LiquidObject liquidObject = await builder.BuildAsync();

            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = vendor.Email;
            var toName = vendor.Name;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendQuantityBelowStoreOwnerMessage(Product product, string languageId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var store = await GetStore("");
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("QuantityBelow.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddProductTokens(product, language, store);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary>
        /// <param name="combination">Attribute combination</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendQuantityBelowStoreOwnerMessage(Product product, ProductAttributeCombination combination, string languageId)
        {
            if (combination == null)
                throw new ArgumentNullException(nameof(combination));

            var store = await GetStore("");
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("QuantityBelow.AttributeCombination.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                    .AddProductTokens(product, language, store)
                    .AddAttributeCombinationTokens(product, combination);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "new VAT sumitted" notification to a store owner
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="vatName">Received VAT name</param>
        /// <param name="vatAddress">Received VAT address</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerDeleteStoreOwnerMessage(Customer customer, string languageId)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var store = await GetStore("");
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("CustomerDelete.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                    .AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a blog comment notification message to a store owner
        /// </summary>
        /// <param name="blogComment">Blog comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendBlogCommentMessage(BlogPost blogPost, BlogComment blogComment, string languageId)
        {
            if (blogComment == null)
                throw new ArgumentNullException(nameof(blogComment));

            var store = await GetStore(blogComment.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("Blog.BlogComment", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddBlogCommentTokens(blogPost, blogComment, store, language);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = blogComment.CustomerId });
            if (customer != null && await _groupService.IsRegistered(customer))
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an article comment notification message to a store owner
        /// </summary>
        /// <param name="articleComment">Article comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendArticleCommentMessage(KnowledgebaseArticle article, KnowledgebaseArticleComment articleComment, string languageId)
        {
            if (articleComment == null)
                throw new ArgumentNullException(nameof(articleComment));

            var store = await GetStore("");
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("Knowledgebase.ArticleComment", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddArticleCommentTokens(article, articleComment, store, language);

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = articleComment.CustomerId });
            if (customer != null && await _groupService.IsRegistered(customer))
                builder.AddCustomerTokens(customer, store, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a news comment notification message to a store owner
        /// </summary>
        /// <param name="newsComment">News comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendNewsCommentMessage(NewsItem newsItem, NewsComment newsComment, string languageId)
        {
            if (newsComment == null)
                throw new ArgumentNullException(nameof(newsComment));

            var store = await GetStore(newsComment.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("News.NewsComment", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddNewsCommentTokens(newsItem, newsComment, store, language);
            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = newsComment.CustomerId });
            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a 'Out of stock' notification message to a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        /// <param name="subscription">Subscription</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendBackinStockMessage(Customer customer, Product product, OutOfStockSubscription subscription, string languageId)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            var store = await GetStore(subscription.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("Customer.OutOfStock", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);
            if (customer != null)
                builder.AddCustomerTokens(customer, store, language);

            builder.AddOutOfStockTokens(product, subscription, store, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        /// <summary>
        /// Sends "contact us" message
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="senderEmail">Sender email</param>
        /// <param name="senderName">Sender name</param>
        /// <param name="subject">Email subject. Pass null if you want a message template subject to be used.</param>
        /// <param name="body">Email body</param>
        /// <param name="attrInfo">Attr info</param>
        /// <param name="customAttributes">CustomAttributes</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendContactUsMessage(Customer customer, Store store, string languageId, string senderEmail,
            string senderName, string subject, string body, string attrInfo, IList<CustomAttribute> customAttributes)
        {
            var language = await EnsureLanguageIsActive(languageId, store.Id);
            var messageTemplate = await GetMessageTemplate("Service.ContactUs", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            string fromEmail;
            string fromName;
            senderName = WebUtility.HtmlEncode(senderName);
            senderEmail = WebUtility.HtmlEncode(senderEmail);
            //required for some SMTP servers
            if (_commonSettings.UseSystemEmailForContactUsForm)
            {
                fromEmail = emailAccount.Email;
                fromName = emailAccount.DisplayName;
            }
            else
            {
                fromEmail = senderEmail;
                fromName = senderName;
            }

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            liquidObject.ContactUs = new LiquidContactUs(senderEmail, senderName, body, attrInfo);
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;

            //store in database
            if (_commonSettings.StoreInDatabaseContactUsForm)
            {
                await _mediator.Send(new InsertContactUsCommand()
                {
                    CustomerId = customer.Id,
                    StoreId = store.Id,
                    VendorId = "",
                    Email = senderEmail,
                    Enquiry = body,
                    FullName = senderName,
                    Subject = string.IsNullOrEmpty(subject) ? "Contact Us (form)" : subject,
                    ContactAttributeDescription = attrInfo,
                    ContactAttributes = customAttributes,
                    EmailAccountId = emailAccount.Id
                });
            }
            return await SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName,
                fromEmail: fromEmail,
                fromName: fromName,
                subject: subject,
                replyToEmailAddress: senderEmail,
                replyToName: senderName);
        }

        /// <summary>
        /// Sends "contact vendor" message
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="senderEmail">Sender email</param>
        /// <param name="senderName">Sender name</param>
        /// <param name="subject">Email subject. Pass null if you want a message template subject to be used.</param>
        /// <param name="body">Email body</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendContactVendorMessage(Customer customer, Store store, Vendor vendor, string languageId, string senderEmail,
            string senderName, string subject, string body)
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("Service.ContactVendor", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            string fromEmail;
            string fromName;
            senderName = WebUtility.HtmlEncode(senderName);
            senderEmail = WebUtility.HtmlEncode(senderEmail);

            //required for some SMTP servers
            if (_commonSettings.UseSystemEmailForContactUsForm)
            {
                fromEmail = emailAccount.Email;
                fromName = emailAccount.DisplayName;

            }
            else
            {
                fromEmail = senderEmail;
                fromName = senderName;
            }

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddCustomerTokens(customer, store, language);
            LiquidObject liquidObject = await builder.BuildAsync();
            liquidObject.ContactUs = new LiquidContactUs(senderEmail, senderName, body, "");
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = vendor.Email;
            var toName = vendor.Name;

            //store in database
            if (_commonSettings.StoreInDatabaseContactUsForm)
            {
                await _mediator.Send(new InsertContactUsCommand()
                {
                    CustomerId = customer.Id,
                    StoreId = store.Id,
                    VendorId = vendor.Id,
                    Email = senderEmail,
                    Enquiry = body,
                    FullName = senderName,
                    Subject = String.IsNullOrEmpty(subject) ? "Contact Us (form)" : subject,
                    EmailAccountId = emailAccount.Id
                });
            }

            return await SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName,
                fromEmail: fromEmail,
                fromName: fromName,
                subject: subject,
                replyToEmailAddress: senderEmail,
                replyToName: senderName);
        }

        #region Customer Action Event

        /// <summary>
        /// Sends a customer action event - Add to cart notification to a customer
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        /// <param name="ShoppingCartItem">Item</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerActionAddToCartMessage(CustomerAction action, ShoppingCartItem cartItem, string languageId, Customer customer)
        {
            if (cartItem == null)
                throw new ArgumentNullException(nameof(cartItem));

            var store = await GetStore(cartItem.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(action.MessageTemplateId);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount);
            //product
            var product = await _mediator.Send(new GetProductByIdQuery() { Id = cartItem.ProductId });
            builder.AddProductTokens(product, language, store);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();

            if (string.IsNullOrEmpty(toEmail))
                return 0;

            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Sends a customer action event - Add to cart notification to a customer
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        /// <param name="Order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerActionAddToOrderMessage(CustomerAction action, Order order, Customer customer, string languageId)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var store = await GetStore(order.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(action.MessageTemplateId);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddOrderTokens(order, customer, store)
                   .AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = string.Empty;
            var toName = string.Empty;

            if (order.BillingAddress != null)
            {
                toEmail = order.BillingAddress.Email;
                toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            }
            else
            {
                if (order.ShippingAddress != null)
                {
                    toEmail = order.ShippingAddress.Email;
                    toName = string.Format("{0} {1}", order.ShippingAddress.FirstName, order.ShippingAddress.LastName);
                }
            }

            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);

        }

        #region Auction notification

        public virtual async Task<int> SendAuctionWinEndedCustomerMessage(Product product, string languageId, Bid bid)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = bid.CustomerId });
            if (customer != null)
            {
                if (string.IsNullOrEmpty(languageId))
                {
                    languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
                }

                var store = await GetStore(bid.StoreId);
                var language = await EnsureLanguageIsActive(languageId, store.Id);

                var messageTemplate = await GetMessageTemplate("AuctionEnded.CustomerNotificationWin", store.Id);
                if (messageTemplate == null)
                    return 0;

                var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

                var builder = new LiquidObjectBuilder(_mediator);
                builder.AddAuctionTokens(product, bid)
                       .AddCustomerTokens(customer, store, language)
                       .AddProductTokens(product, language, store)
                       .AddStoreTokens(store, language, emailAccount);

                LiquidObject liquidObject = await builder.BuildAsync();
                //event notification
                await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

                var toEmail = customer.Email;
                var toName = customer.GetFullName();
                return await SendNotification(messageTemplate, emailAccount,
                    languageId, liquidObject,
                    toEmail, toName);
            }
            return 0;
        }

        public virtual async Task<int> SendAuctionEndedLostCustomerMessage(Product product, string languageId, Bid bid)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var customerwin = await _mediator.Send(new GetCustomerByIdQuery() { Id = bid.CustomerId });
            if (customerwin != null)
            {
                var store = await GetStore(bid.StoreId);

                var language = await EnsureLanguageIsActive(languageId, store.Id);

                var messageTemplate = await GetMessageTemplate("AuctionEnded.CustomerNotificationLost", store.Id);
                if (messageTemplate == null)
                    return 0;

                var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

                var builder = new LiquidObjectBuilder(_mediator);
                builder.AddAuctionTokens(product, bid)
                       .AddProductTokens(product, language, store)
                       .AddStoreTokens(store, language, emailAccount);
                LiquidObject liquidObject = await builder.BuildAsync();

                var bids = (await _mediator.Send(new GetBidsByProductIdQuery() { ProductId = bid.ProductId })).Where(x => x.CustomerId != bid.CustomerId).GroupBy(x => x.CustomerId);
                foreach (var item in bids)
                {
                    var builder2 = new LiquidObjectBuilder(_mediator, liquidObject);
                    var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = item.Key });
                    if (string.IsNullOrEmpty(languageId))
                    {
                        languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
                    }
                    builder2.AddCustomerTokens(customer, store, language);
                    LiquidObject liquidObject2 = await builder2.BuildAsync();

                    //event notification
                    await _mediator.MessageTokensAdded(messageTemplate, liquidObject2);

                    var toEmail = customer.Email;
                    var toName = customer.GetFullName();
                    await SendNotification(messageTemplate, emailAccount,
                        languageId, liquidObject2,
                        toEmail, toName);
                }
            }
            return 0;
        }

        public virtual async Task<int> SendAuctionEndedBinCustomerMessage(Product product, string customerId, string languageId, string storeId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var store = await GetStore(storeId);

            var messageTemplate = await GetMessageTemplate("AuctionEnded.CustomerNotificationBin", storeId);
            if (messageTemplate == null)
                return 0;

            var language = await EnsureLanguageIsActive(languageId, store.Id);
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddProductTokens(product, language, store)
                   .AddStoreTokens(store, language, emailAccount);

            LiquidObject liquidObject = await builder.BuildAsync();
            var bids = (await _mediator.Send(new GetBidsByProductIdQuery() { ProductId = product.Id })).Where(x => x.CustomerId != customerId).GroupBy(x => x.CustomerId);
            foreach (var item in bids)
            {
                var builder2 = new LiquidObjectBuilder(_mediator, liquidObject);
                var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = item.Key });
                if (customer != null)
                {
                    if (string.IsNullOrEmpty(languageId))
                    {
                        languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
                    }

                    builder2.AddCustomerTokens(customer, store, language);
                    LiquidObject liquidObject2 = await builder2.BuildAsync();
                    //event notification
                    await _mediator.MessageTokensAdded(messageTemplate, liquidObject2);

                    var toEmail = customer.Email;
                    var toName = customer.GetFullName();
                    await SendNotification(messageTemplate, emailAccount,
                        languageId, liquidObject2,
                        toEmail, toName);
                }
            }

            return 0;
        }
        public virtual async Task<int> SendAuctionEndedStoreOwnerMessage(Product product, string languageId, Bid bid)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var builder = new LiquidObjectBuilder(_mediator);
            MessageTemplate messageTemplate = null;
            EmailAccount emailAccount = null;

            if (bid != null)
            {
                var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = bid.CustomerId });
                if (string.IsNullOrEmpty(languageId))
                {
                    languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
                }

                var store = await GetStore(bid.StoreId);

                var language = await EnsureLanguageIsActive(languageId, store.Id);

                messageTemplate = await GetMessageTemplate("AuctionEnded.StoreOwnerNotification", store.Id);
                if (messageTemplate == null)
                    return 0;

                emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);
                builder.AddAuctionTokens(product, bid)
                        .AddCustomerTokens(customer, store, language)
                        .AddStoreTokens(store, language, emailAccount);
            }
            else
            {
                var store = (await _storeService.GetAllStores()).FirstOrDefault();
                var language = await EnsureLanguageIsActive(languageId, store.Id);
                messageTemplate = await GetMessageTemplate("AuctionExpired.StoreOwnerNotification", "");
                if (messageTemplate == null)
                    return 0;

                emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);
                builder.AddProductTokens(product, language, store);
            }
            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;

            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }


        /// <summary>
        /// Send outbid notification to a customer
        /// </summary>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product</param>
        /// <param name="Bid">Bid</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendOutBidCustomerMessage(Product product, string languageId, Bid bid)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var customer = await _mediator.Send(new GetCustomerByIdQuery() { Id = bid.CustomerId });
            if (string.IsNullOrEmpty(languageId))
            {
                languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
            }

            var store = await GetStore(bid.StoreId);

            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await GetMessageTemplate("BidUp.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddAuctionTokens(product, bid)
                   .AddCustomerTokens(customer, store, language)
                   .AddStoreTokens(store, language, emailAccount);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }
        #endregion



        /// <summary>
        /// Sends a customer action event - Add to cart notification to a customer
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual async Task<int> SendCustomerActionMessage(CustomerAction action, string languageId, Customer customer)
        {
            var store = await GetStore(customer.StoreId);
            var language = await EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = await _messageTemplateService.GetMessageTemplateById(action.MessageTemplateId);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

            var builder = new LiquidObjectBuilder(_mediator);
            builder.AddStoreTokens(store, language, emailAccount)
                   .AddCustomerTokens(customer, store, language);

            LiquidObject liquidObject = await builder.BuildAsync();
            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();

            if (string.IsNullOrEmpty(toEmail))
                return 0;

            return await SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject,
                toEmail, toName);
        }

        #endregion

        public virtual async Task<int> SendNotification(MessageTemplate messageTemplate,
            EmailAccount emailAccount, string languageId, LiquidObject liquidObject,
            string toEmailAddress, string toName,
            string attachmentFilePath = null, string attachmentFileName = null,
            IEnumerable<string> attachedDownloads = null,
            string replyToEmailAddress = null, string replyToName = null,
            string fromEmail = null, string fromName = null, string subject = null)
        {
            if (String.IsNullOrEmpty(toEmailAddress))
                return 0;

            //retrieve translation message template data
            var bcc = messageTemplate.GetTranslation(mt => mt.BccEmailAddresses, languageId);

            if (String.IsNullOrEmpty(subject))
                subject = messageTemplate.GetTranslation(mt => mt.Subject, languageId);

            var body = messageTemplate.GetTranslation(mt => mt.Body, languageId);

            var email = new QueuedEmail();
            liquidObject.Email = new LiquidEmail(email.Id);

            var subjectReplaced = LiquidExtensions.Render(liquidObject, subject);
            var bodyReplaced = LiquidExtensions.Render(liquidObject, body);

            var attachments = new List<string>();
            if (attachedDownloads != null)
                attachments.AddRange(attachedDownloads);
            if (!string.IsNullOrEmpty(messageTemplate.AttachedDownloadId))
                attachments.Add(messageTemplate.AttachedDownloadId);

            //limit name length
            toName = CommonHelper.EnsureMaximumLength(toName, 300);
            email.PriorityId = QueuedEmailPriority.High;
            email.From = !string.IsNullOrEmpty(fromEmail) ? fromEmail : emailAccount.Email;
            email.FromName = !string.IsNullOrEmpty(fromName) ? fromName : emailAccount.DisplayName;
            email.To = toEmailAddress;
            email.ToName = toName;
            email.ReplyTo = replyToEmailAddress;
            email.ReplyToName = replyToName;
            email.CC = string.Empty;
            email.Bcc = bcc;
            email.Subject = subjectReplaced;
            email.Body = bodyReplaced;
            email.AttachmentFilePath = attachmentFilePath;
            email.AttachmentFileName = attachmentFileName;
            email.AttachedDownloads = attachments;
            email.CreatedOnUtc = DateTime.UtcNow;
            email.EmailAccountId = emailAccount.Id;
            email.DontSendBeforeDateUtc = !messageTemplate.DelayBeforeSend.HasValue ? null
                    : (DateTime?)(DateTime.UtcNow + TimeSpan.FromHours(messageTemplate.DelayPeriodId.ToHours(messageTemplate.DelayBeforeSend.Value)));

            await _queuedEmailService.InsertQueuedEmail(email);
            return 1;
        }

        #endregion

        #endregion
    }
}
