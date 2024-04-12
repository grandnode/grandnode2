using Grand.Domain;
using Grand.Domain.Knowledgebase;

namespace Grand.Business.Core.Interfaces.Cms;

public interface IKnowledgebaseService
{
    /// <summary>
    ///     Gets knowledge base category
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base category</returns>
    Task<KnowledgebaseCategory> GetKnowledgebaseCategory(string id);

    /// <summary>
    ///     Gets public knowledge base category
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base category</returns>
    Task<KnowledgebaseCategory> GetPublicKnowledgebaseCategory(string id);

    /// <summary>
    ///     Gets knowledge base categories
    /// </summary>
    /// <returns>List of knowledge base categories</returns>
    Task<List<KnowledgebaseCategory>> GetKnowledgebaseCategories();

    /// <summary>
    ///     Gets public(published etc) knowledge base categories
    /// </summary>
    /// <returns>List of public knowledge base categories</returns>
    Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategories();

    /// <summary>
    ///     Inserts knowledge base category
    /// </summary>
    /// <param name="kc"></param>
    Task InsertKnowledgebaseCategory(KnowledgebaseCategory kc);

    /// <summary>
    ///     Updates knowledge base category
    /// </summary>
    /// <param name="kc"></param>
    Task UpdateKnowledgebaseCategory(KnowledgebaseCategory kc);

    /// <summary>
    ///     Deletes knowledge base category
    /// </summary>
    /// <param name="kc"></param>
    Task DeleteKnowledgebaseCategory(KnowledgebaseCategory kc);

    /// <summary>
    ///     Gets knowledge base article
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base article</returns>
    Task<KnowledgebaseArticle> GetKnowledgebaseArticle(string id);

    /// <summary>
    ///     Gets public knowledge base article
    /// </summary>
    /// <param name="id"></param>
    /// <returns>knowledge base article</returns>
    Task<KnowledgebaseArticle> GetPublicKnowledgebaseArticle(string id);

    /// <summary>
    ///     Gets knowledge base articles
    /// </summary>
    /// <returns>List of knowledge base articles</returns>
    /// <param name="storeId">Store ident</param>
    Task<List<KnowledgebaseArticle>> GetKnowledgebaseArticles(string storeId = "");

    /// <summary>
    ///     Gets public(published etc) knowledge base articles
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticles();

    /// <summary>
    ///     Gets homepage knowledge base articles
    /// </summary>
    /// <returns>List of homepage knowledge base articles</returns>
    Task<List<KnowledgebaseArticle>> GetHomepageKnowledgebaseArticles();

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for category id
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByCategory(string categoryId);

    /// <summary>
    ///     Gets public(published etc) knowledge base articles for keyword
    /// </summary>
    /// <returns>List of public knowledge base articles</returns>
    Task<List<KnowledgebaseArticle>> GetPublicKnowledgebaseArticlesByKeyword(string keyword);

    /// <summary>
    ///     Gets public(published etc) knowledge base categories for keyword
    /// </summary>
    /// <returns>List of public knowledge base categories</returns>
    Task<List<KnowledgebaseCategory>> GetPublicKnowledgebaseCategoriesByKeyword(string keyword);

    /// <summary>
    ///     Inserts knowledge base article
    /// </summary>
    /// <param name="ka"></param>
    Task InsertKnowledgebaseArticle(KnowledgebaseArticle ka);

    /// <summary>
    ///     Updates knowledge base article
    /// </summary>
    /// <param name="ka"></param>
    Task UpdateKnowledgebaseArticle(KnowledgebaseArticle ka);

    /// <summary>
    ///     Deletes knowledge base article
    /// </summary>
    /// <param name="ka"></param>
    Task DeleteKnowledgebaseArticle(KnowledgebaseArticle ka);

    /// <summary>
    ///     Gets knowledge base articles by category id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByCategoryId(string id, int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets knowledge base articles by name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    Task<IPagedList<KnowledgebaseArticle>> GetKnowledgebaseArticlesByName(string name, int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Gets related knowledge base articles
    /// </summary>
    Task<IPagedList<KnowledgebaseArticle>> GetRelatedKnowledgebaseArticles(string articleId, int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Inserts an article comment
    /// </summary>
    /// <param name="articleComment">Article comment</param>
    Task InsertArticleComment(KnowledgebaseArticleComment articleComment);

    Task<IList<KnowledgebaseArticleComment>> GetArticleCommentsByArticleId(string articleId);
    Task DeleteArticleComment(KnowledgebaseArticleComment articleComment);
}