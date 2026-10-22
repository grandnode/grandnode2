using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Blogs;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class CmsMessageProviderService : ICmsMessageProviderService
{
    #region Ctor

    public CmsMessageProviderService(
        IMessageLookupService messageLookupService,
        IMessageProviderService messageProviderService,
        IMediator mediator,
        IGroupService groupService)
    {
        _messageLookupService = messageLookupService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
        _groupService = groupService;
    }

    #endregion

    #region Fields

    private readonly IMessageLookupService _messageLookupService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IMediator _mediator;
    private readonly IGroupService _groupService;

    #endregion

    #region Methods

    /// <summary>
    ///     Sends a blog comment notification message to a store owner
    /// </summary>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendBlogCommentMessage(BlogPost blogPost, BlogComment blogComment, string languageId,
        DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(blogComment);

        var store = await _messageLookupService.GetStore(blogComment.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("Blog.BlogComment", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddBlogCommentTokens(blogPost, blogComment, store, host, language);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = blogComment.CustomerId });
        if (customer != null && await _groupService.IsRegistered(customer))
            builder.AddCustomerTokens(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Blog, objectId: blogPost.Id);
    }

    /// <summary>
    ///     Sends an article comment notification message to a store owner
    /// </summary>
    /// <param name="article"></param>
    /// <param name="articleComment">Article comment</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendArticleCommentMessage(KnowledgebaseArticle article,
        KnowledgebaseArticleComment articleComment, string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(articleComment);

        var store = await _messageLookupService.GetStore("");
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("Knowledgebase.ArticleComment", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddArticleCommentTokens(article, articleComment, store, host, language);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = articleComment.CustomerId });
        if (customer != null && await _groupService.IsRegistered(customer))
            builder.AddCustomerTokens(customer, store, host, language);
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
    ///     Sends a news comment notification message to a store owner
    /// </summary>
    /// <param name="newsItem">News item</param>
    /// <param name="newsComment">News comment</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewsCommentMessage(NewsItem newsItem, NewsComment newsComment, string languageId,
        DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(newsComment);

        var store = await _messageLookupService.GetStore(newsComment.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("News.NewsComment", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddNewsCommentTokens(newsItem, newsComment, store, host, language);
        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = newsComment.CustomerId });
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName);
    }

    #endregion
}
