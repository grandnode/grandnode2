using DotLiquid;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidKnowledgebase : Drop
    {
        private readonly KnowledgebaseArticle _article;
        private readonly KnowledgebaseArticleComment _articleComment;
        private readonly Store _store;
        private readonly DomainHost _host;
        private readonly Language _language;

        private string url;

        public LiquidKnowledgebase(KnowledgebaseArticle article, KnowledgebaseArticleComment articleComment, Store store, DomainHost host, Language language)
        {
            _article = article;
            _articleComment = articleComment;
            _store = store;
            _host = host;
            _language = language;

            url = _host?.Url.Trim('/') ?? (_store.SslEnabled ? _store.SecureUrl.Trim('/') : _store.Url.Trim('/'));

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string ArticleTitle {
            get { return _article.Name; }
        }
        public string ArticleCommentText {
            get { return _articleComment.CommentText; }
        }

        public string ArticleCommentUrl {
            get {
                return $"{url}/{_article.SeName}";
            }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}