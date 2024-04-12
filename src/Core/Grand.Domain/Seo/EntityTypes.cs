using Grand.Domain.Knowledgebase;

namespace Grand.Domain.Seo;

public struct EntityTypes
{
    public const string Product = nameof(Catalog.Product);
    public const string Category = nameof(Catalog.Category);
    public const string Brand = nameof(Catalog.Brand);
    public const string Collection = nameof(Catalog.Collection);
    public const string Vendor = nameof(Vendors.Vendor);
    public const string NewsItem = nameof(News.NewsItem);
    public const string BlogPost = nameof(Blogs.BlogPost);
    public const string Page = nameof(Pages.Page);
    public const string KnowledgeBaseArticle = nameof(KnowledgebaseArticle);
    public const string KnowledgeBaseCategory = nameof(KnowledgebaseCategory);
    public const string Course = nameof(Courses.Course);
}