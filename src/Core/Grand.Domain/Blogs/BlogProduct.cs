﻿namespace Grand.Domain.Blogs
{
    public class BlogProduct : BaseEntity
    {
        public string BlogPostId { get; set; }
        public string ProductId { get; set; }
        public int DisplayOrder { get; set; }
    }
}
