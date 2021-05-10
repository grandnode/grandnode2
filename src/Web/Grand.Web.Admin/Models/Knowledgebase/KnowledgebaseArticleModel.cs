using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Knowledgebase
{
    public class KnowledgebaseArticleModel : BaseEntityModel, ILocalizedModel<KnowledgebaseArticleLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public KnowledgebaseArticleModel()
        {
            Categories = new List<SelectListItem>();
            Locales = new List<KnowledgebaseArticleLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Content")]
        public string Content { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId")]
        public string ParentCategoryId { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Published")]
        public bool Published { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.SeName")]
        public string SeName { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ShowOnHomepage")]
        public bool ShowOnHomepage { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.AllowComments")]
        public bool AllowComments { get; set; }

        public List<SelectListItem> Categories { get; set; }

        public IList<KnowledgebaseArticleLocalizedModel> Locales { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        public partial class ActivityLogModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Content.Knowledgebase.ActivityLogType")]
            public string ActivityLogTypeName { get; set; }
            [GrandResourceDisplayName("Admin.Content.Knowledgebase.ActivityLog.Comment")]
            public string Comment { get; set; }
            [GrandResourceDisplayName("Admin.Content.Knowledgebase.ActivityLog.CreatedOn")]
            public DateTime CreatedOn { get; set; }
            [GrandResourceDisplayName("Admin.Content.Knowledgebase.ActivityLog.Customer")]
            public string CustomerId { get; set; }
            public string CustomerEmail { get; set; }
        }

        public partial class AddRelatedArticleModel : BaseEntityModel
        {
            public AddRelatedArticleModel()
            {
                AvailableArticles = new List<SelectListItem>();
            }

            [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Related.SearchArticleName")]
            public string SearchArticleName { get; set; }

            public string ArticleId { get; set; }

            public string[] SelectedArticlesIds { get; set; }

            public IList<SelectListItem> AvailableArticles { get; set; }
        }
    }

    public class KnowledgebaseArticleLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.Content")]
        public string Content { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.SeName")]
        public string SeName { get; set; }
    }
}
