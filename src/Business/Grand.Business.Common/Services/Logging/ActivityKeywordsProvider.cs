using Grand.Business.Common.Interfaces.Logging;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Logging
{
    public class ActivityKeywordsProvider: IActivityKeywordsProvider
    {
        public virtual IList<string> GetCategorySystemKeywords()
        {
            var tokens = new List<string>
            {
                "PublicStore.ViewCategory",
                "EditCategory",
                "AddNewCategory",
            };
            return tokens;
        }
        public virtual IList<string> GetProductSystemKeywords()
        {
            var tokens = new List<string>
            {
                "PublicStore.ViewProduct",
                "EditProduct",
                "AddNewProduct",
            };
            return tokens;
        }
        public virtual IList<string> GetBrandSystemKeywords()
        {
            var tokens = new List<string>
            {
                "PublicStore.ViewBrand",
                "EditBrand",
                "AddNewBrand"
            };
            return tokens;
        }

        public virtual IList<string> GetCollectionSystemKeywords()
        {
            var tokens = new List<string>
            {
                "PublicStore.ViewCollection",
                "EditCollection",
                "AddNewCollection"
            };
            return tokens;
        }

        public IList<string> GetKnowledgebaseCategorySystemKeywords()
        {
            var tokens = new List<string>
            {
                "CreateKnowledgebaseCategory",
                "UpdateKnowledgebaseCategory",
                "DeleteKnowledgebaseCategory"
            };
            return tokens;
        }


        public IList<string> GetKnowledgebaseArticleSystemKeywords()
        {
            var tokens = new List<string>
            {
                "CreateKnowledgebaseArticle",
                "UpdateKnowledgebaseArticle",
                "DeleteKnowledgebaseArticle",
            };
            return tokens;
        }
    }
}
