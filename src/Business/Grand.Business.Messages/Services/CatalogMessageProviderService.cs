using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class CatalogMessageProviderService : ICatalogMessageProviderService
{
    #region Ctor

    public CatalogMessageProviderService(
        IMessageLookupService messageLookupService,
        IMessageProviderService messageProviderService,
        IMediator mediator,
        IStoreService storeService,
        CommonSettings commonSettings)
    {
        _messageLookupService = messageLookupService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
        _storeService = storeService;
        _commonSettings = commonSettings;
    }

    #endregion

    #region Fields

    private readonly IMessageLookupService _messageLookupService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IMediator _mediator;
    private readonly IStoreService _storeService;
    private readonly CommonSettings _commonSettings;

    #endregion

    #region Methods

    /// <summary>
    ///     Sends "email a friend" message
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="product">Product instance</param>
    /// <param name="customerEmail">Customer's email</param>
    /// <param name="friendsEmail">Friend's email</param>
    /// <param name="personalMessage">Personal message</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendProductEmailAFriendMessage(Customer customer, Store store, string languageId,
        Product product, string customerEmail, string friendsEmail, string personalMessage, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendProductEmailAFriendMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddEmailAFriendTokens(personalMessage, customerEmail, friendsEmail)
            .AddCustomerTokens(customer, store, host, language)
            .AddProductTokens(product, language, store, host);
        var liquidObject = await builder.BuildAsync();

        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = friendsEmail;
        var toName = "";
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer.Id);
    }

    /// <summary>
    ///     Sends wishlist "email a friend" message
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="customerEmail">Customer's email</param>
    /// <param name="friendsEmail">Friend's email</param>
    /// <param name="personalMessage">Personal message</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendWishlistEmailAFriendMessage(Customer customer, Store store, string languageId,
        string customerEmail, string friendsEmail, string personalMessage, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendWishlistEmailAFriendMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);


        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(customer, store, host, language)
            .AddEmailAFriendTokens(personalMessage, customerEmail, friendsEmail);

        var liquidObject = await builder.BuildAsync();

        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = friendsEmail;
        var toName = "";
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer.Id);
    }

    /// <summary>
    ///     Sends "email a friend" message
    /// </summary>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendProductQuestionMessage(Customer customer, Store store, string languageId,
        Product product, string customerEmail, string fullName, string phone, string message, string ipaddress,
            DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendProductQuestionMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(customer, store, host, language)
            .AddProductTokens(product, language, store, host);
        var liquidObject = await builder.BuildAsync();
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

            await _mediator.Send(new InsertContactUsCommand {
                CustomerId = customer.Id,
                StoreId = store.Id,
                VendorId = product.VendorId,
                Email = customerEmail,
                Enquiry = bodyReplaced,
                FullName = fullName,
                Subject = subjectReplaced,
                EmailAccountId = emailAccount.Id,
                RemoteIpAddress = ipaddress
            });
        }

        var toEmail = emailAccount.Email;
        var toName = "";

        if (!string.IsNullOrEmpty(product.VendorId))
        {
            var vendor = await _mediator.Send(new GetVendorByIdQuery { Id = product.VendorId });
            if (vendor != null)
            {
                toEmail = vendor.Email;
                toName = vendor.Name;
            }
        }

        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName, replyToEmailAddress: customerEmail,
            reference: Reference.Customer, objectId: customer.Id);
    }

    /// <summary>
    ///     Sends a product review notification message to a store owner
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="productReview">Product review</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendProductReviewMessage(Product product, ProductReview productReview,
        Store store, string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(productReview);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("Product.ProductReview", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = productReview.CustomerId });

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddProductReviewTokens(product, productReview);
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Product, objectId: product.Id);
    }

    /// <summary>
    ///     Sends a "quantity below" notification to a store owner
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendQuantityBelowStoreOwnerMessage(Product product, string languageId)
    {
        ArgumentNullException.ThrowIfNull(product);

        var store = await _messageLookupService.GetStore("");
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("QuantityBelow.StoreOwnerNotification", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddProductTokens(product, language, store, host);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Product, objectId: product.Id);
    }

    /// <summary>
    ///     Sends a "quantity below" notification to a store owner
    /// </summary>
    /// <param name="product"></param>
    /// <param name="combination">Attribute combination</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendQuantityBelowStoreOwnerMessage(Product product,
        ProductAttributeCombination combination, string languageId)
    {
        ArgumentNullException.ThrowIfNull(combination);

        var store = await _messageLookupService.GetStore("");
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate =
            await _messageLookupService.GetMessageTemplate("QuantityBelow.AttributeCombination.StoreOwnerNotification", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddProductTokens(product, language, store, host)
            .AddAttributeCombinationTokens(product, combination, language, host);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Product, objectId: product.Id);
    }

    /// <summary>
    ///     Sends a 'Out of stock' notification message to a customer
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    /// <param name="subscription">Subscription</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendBackInStockMessage(Customer customer, Product product,
        OutOfStockSubscription subscription, string languageId)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var store = await _messageLookupService.GetStore(subscription.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("Customer.OutOfStock", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount);
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        builder.AddOutOfStockTokens(product, subscription, store, host, language);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = customer?.Email;
        var toName = customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer?.Id);
    }

    public virtual async Task<int> SendAuctionWinEndedCustomerMessage(Product product, string languageId, Bid bid)
    {
        ArgumentNullException.ThrowIfNull(product);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = bid.CustomerId });
        if (customer == null) return 0;
        if (string.IsNullOrEmpty(languageId))
            languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);

        var store = await _messageLookupService.GetStore(bid.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("AuctionEnded.CustomerNotificationWin", store.Id);
        if (messageTemplate == null)
            return 0;

        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddAuctionTokens(product, bid)
            .AddCustomerTokens(customer, store, host, language)
            .AddProductTokens(product, language, store, host)
            .AddStoreTokens(store, language, emailAccount);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = customer.Email;
        var toName = customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer.Id);
    }

    public virtual async Task<int> SendAuctionEndedLostCustomerMessage(Product product, string languageId, Bid bid)
    {
        ArgumentNullException.ThrowIfNull(product);

        var winner = await _mediator.Send(new GetCustomerByIdQuery { Id = bid.CustomerId });
        if (winner == null) return 0;

        var store = await _messageLookupService.GetStore(bid.StoreId);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("AuctionEnded.CustomerNotificationLost", store.Id);
        if (messageTemplate == null)
            return 0;

        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddAuctionTokens(product, bid)
            .AddProductTokens(product, language, store, host)
            .AddStoreTokens(store, language, emailAccount);
        var liquidObject = await builder.BuildAsync();

        var bids = (await _mediator.Send(new GetBidsByProductIdQuery { ProductId = bid.ProductId }))
            .Where(x => x.CustomerId != bid.CustomerId).GroupBy(x => x.CustomerId);
        foreach (var item in bids)
        {
            var builder2 = new LiquidObjectBuilder(_mediator, liquidObject);
            var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = item.Key });
            if (string.IsNullOrEmpty(languageId))
                languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);
            builder2.AddCustomerTokens(customer, store, host, language);
            var liquidObject2 = await builder2.BuildAsync();

            //event notification
            await _mediator.MessageTokensAdded(messageTemplate, liquidObject2);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            await _messageProviderService.SendNotification(messageTemplate, emailAccount,
                languageId, liquidObject2,
                toEmail, toName,
                reference: Reference.Customer, objectId: customer.Id);
        }

        return 0;
    }

    public virtual async Task<int> SendAuctionEndedBinCustomerMessage(Product product, string customerId,
        string languageId, string storeId)
    {
        ArgumentNullException.ThrowIfNull(product);

        var store = await _messageLookupService.GetStore(storeId);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("AuctionEnded.CustomerNotificationBin", storeId);
        if (messageTemplate == null)
            return 0;

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddProductTokens(product, language, store, host)
            .AddStoreTokens(store, language, emailAccount);

        var liquidObject = await builder.BuildAsync();
        var bids = (await _mediator.Send(new GetBidsByProductIdQuery { ProductId = product.Id }))
            .Where(x => x.CustomerId != customerId).GroupBy(x => x.CustomerId);
        foreach (var item in bids)
        {
            var builder2 = new LiquidObjectBuilder(_mediator, liquidObject);
            var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = item.Key });
            if (customer != null)
            {
                if (string.IsNullOrEmpty(languageId))
                    languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);

                builder2.AddCustomerTokensIfNotNull(customer, store, host, language);
                var liquidObject2 = await builder2.BuildAsync();
                //event notification
                await _mediator.MessageTokensAdded(messageTemplate, liquidObject2);

                var toEmail = customer.Email;
                var toName = customer.GetFullName();
                await _messageProviderService.SendNotification(messageTemplate, emailAccount,
                    languageId, liquidObject2,
                    toEmail, toName,
                    reference: Reference.Customer, objectId: customer.Id);
            }
        }

        return 0;
    }

    public virtual async Task<int> SendAuctionEndedStoreOwnerMessage(Product product, string languageId, Bid bid)
    {
        ArgumentNullException.ThrowIfNull(product);

        var builder = new LiquidObjectBuilder(_mediator);
        MessageTemplate messageTemplate;
        EmailAccount emailAccount;

        if (bid != null)
        {
            var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = bid.CustomerId });
            if (string.IsNullOrEmpty(languageId))
                languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);

            var store = await _messageLookupService.GetStore(bid.StoreId);

            var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

            messageTemplate = await _messageLookupService.GetMessageTemplate("AuctionEnded.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);
            builder.AddAuctionTokens(product, bid)
                .AddCustomerTokens(customer, store, store.PrimaryHost(), language)
                .AddStoreTokens(store, language, emailAccount);
        }
        else
        {
            var store = (await _storeService.GetAllStores()).FirstOrDefault();
            var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store?.Id);
            messageTemplate = await _messageLookupService.GetMessageTemplate("AuctionExpired.StoreOwnerNotification", "");
            if (messageTemplate == null)
                return 0;

            emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);
            builder.AddProductTokens(product, language, store, store.PrimaryHost());
        }

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;

        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName);
    }

    /// <summary>
    ///     Send outbid notification to a customer
    /// </summary>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="product">Product</param>
    /// <param name="bid"></param>
    public virtual async Task<int> SendOutBidCustomerMessage(Product product, string languageId, Bid bid)
    {
        ArgumentNullException.ThrowIfNull(product);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = bid.CustomerId });
        if (string.IsNullOrEmpty(languageId))
            languageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId);

        var store = await _messageLookupService.GetStore(bid.StoreId);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("BidUp.CustomerNotification", store.Id);
        if (messageTemplate == null)
            return 0;

        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddAuctionTokens(product, bid)
            .AddCustomerTokens(customer, store, host, language)
            .AddStoreTokens(store, language, emailAccount);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = customer.Email;
        var toName = customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer.Id);
    }

    #endregion
}
