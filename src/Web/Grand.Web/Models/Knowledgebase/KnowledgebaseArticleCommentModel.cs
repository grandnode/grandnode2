﻿using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Knowledgebase
{
    public class KnowledgebaseArticleCommentModel : BaseEntityModel
    {
        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CommentText { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
