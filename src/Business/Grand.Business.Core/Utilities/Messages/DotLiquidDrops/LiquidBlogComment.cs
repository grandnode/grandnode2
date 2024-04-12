using DotLiquid;
using Grand.Domain.Blogs;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidBlogComment : Drop
{
    private readonly BlogComment _blogComment;
    private readonly BlogPost _blogPost;
    private readonly DomainHost _host;
    private readonly Language _language;
    private readonly Store _store;

    private readonly string url;

    public LiquidBlogComment(BlogComment blogComment, BlogPost blogPost, Store store, DomainHost host,
        Language language)
    {
        _blogComment = blogComment;
        _blogPost = blogPost;
        _store = store;
        _host = host;
        _language = language;

        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string BlogPostTitle => _blogPost.Title;

    public string BlogPostURL => $"{url}/{_blogPost.SeName}";

    public string BlogPostCommentText => _blogComment?.CommentText;

    public IDictionary<string, string> AdditionalTokens { get; set; }
}