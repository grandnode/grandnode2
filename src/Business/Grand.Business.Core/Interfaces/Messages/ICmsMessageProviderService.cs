using Grand.Domain.Blogs;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Messages;

public interface ICmsMessageProviderService
{
    /// <summary>
    ///     Sends a blog comment notification message to a store owner
    /// </summary>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendBlogCommentMessage(BlogPost blogPost, BlogComment blogComment, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends an article comment notification message to a store owner
    /// </summary>
    /// <param name="article">Article</param>
    /// <param name="articleComment">Article comment</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendArticleCommentMessage(KnowledgebaseArticle article, KnowledgebaseArticleComment articleComment,
        string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends a news comment notification message to a store owner
    /// </summary>
    /// <param name="newsItem">News item</param>
    /// <param name="newsComment">News comment</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewsCommentMessage(NewsItem newsItem, NewsComment newsComment, string languageId,
        DomainHost host = null);
}
