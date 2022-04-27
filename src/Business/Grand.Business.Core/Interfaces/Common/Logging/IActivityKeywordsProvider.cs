namespace Grand.Business.Core.Interfaces.Common.Logging
{
    public partial interface IActivityKeywordsProvider
    {
        IList<string> GetCategorySystemKeywords();
        IList<string> GetProductSystemKeywords();
        IList<string> GetBrandSystemKeywords();
        IList<string> GetCollectionSystemKeywords();
        IList<string> GetKnowledgebaseCategorySystemKeywords();
        IList<string> GetKnowledgebaseArticleSystemKeywords();
    }
}
