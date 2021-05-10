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
    public class KnowledgebaseCategoryModel : BaseEntityModel, ILocalizedModel<KnowledgebaseCategoryLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public KnowledgebaseCategoryModel()
        {
            Categories = new List<SelectListItem>();
            Locales = new List<KnowledgebaseCategoryLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.ParentCategoryId")]
        public string ParentCategoryId { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Published")]
        public bool Published { get; set; }

        public List<SelectListItem> Categories { get; set; }

        public IList<KnowledgebaseCategoryLocalizedModel> Locales { get; set; }

        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.SeName")]
        public string SeName { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }
        public bool LimitedToStores { get; set; }

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
    }

    public class KnowledgebaseCategoryLocalizedModel : ILocalizedModelLocal, ISlugModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.MetaKeywords")]
        public string MetaKeywords { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.MetaDescription")]
        public string MetaDescription { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.MetaTitle")]
        public string MetaTitle { get; set; }

        [GrandResourceDisplayName("Admin.Content.Knowledgebase.KnowledgebaseCategory.Fields.SeName")]
        public string SeName { get; set; }
    }
}
