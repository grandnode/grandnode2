using DotLiquid;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidNewsComment : Drop
{
    private readonly DomainHost _host;
    private readonly Language _language;
    private readonly NewsComment _newsComment;
    private readonly NewsItem _newsItem;
    private readonly Store _store;

    private readonly string url;

    public LiquidNewsComment(NewsItem newsItem, NewsComment newsComment, Store store, DomainHost host,
        Language language)
    {
        _newsComment = newsComment;
        _newsItem = newsItem;
        _store = store;
        _language = language;
        _host = host;

        url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

        AdditionalTokens = new Dictionary<string, string>();
    }

    public string NewsTitle => _newsItem.Title;

    public string CommentText => _newsComment.CommentText;

    public string CommentTitle => _newsComment.CommentTitle;

    public string NewsURL => $"{url}/{_newsItem.SeName}";

    public IDictionary<string, string> AdditionalTokens { get; set; }
}