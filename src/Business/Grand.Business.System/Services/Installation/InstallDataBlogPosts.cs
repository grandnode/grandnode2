﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Blogs;
using Grand.Domain.Seo;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService
    {
        protected virtual async Task InstallBlogPosts()
        {
            var blogPosts = new List<BlogPost>
                                {
                                    new BlogPost
                                        {
                                             AllowComments = false,
                                             Title = "How GrandNode became the real open-source e-Commerce platform?",
                                             BodyOverview = "<p>There are many e-commerce solutions on the market. Is everyone open-source? No, and even if they say yes, it may turn out to be a lie. GrandNode is a true open-source based on .NET Core and MongoDB. No hidden license fees, no additional licensing conditions. Pure open-source and GPL-3 license.</p>",
                                             Body = "<p>There are many e-commerce solutions on the market. Is every open-source? No, and even if they say yes, it may turn out to be a lie. GrandNode is a true open-source platform based on .NET Core and MongoDB. No hidden license fees, no additional licensing conditions. Pure open-source and GPL-3 license.</p><p>An open-source platform shouldn't block your development. Forcing the leaving of information about the platform, paid licenses, obtaining paid license keys is an artificial invention that makes the platform paid and complicated.</p>",
                                             Tags = "e-commerce, blog, moey",
                                             CreatedOnUtc = DateTime.UtcNow,
                                        },
                                    new BlogPost
                                        {
                                             AllowComments = false,
                                             Title = "Open-source e-commerce platform GrandNode",
                                             BodyOverview = "<p>GrandNode is one of a kind open-source e-commerce platform. Thanks to most advanced ASP.NET Core framework and out of the box set of features it makes a perfect fit for multiple e-business models.</p>",
                                             Body = "<p>Meet the flexible, the most versatile open-source e-commerce platform on the market. Use GrandNode and its ready-to-use integrations to sell everywhere with a single platform. GrandNode guarantees functionalities for now and for the future. We ensure both, the base needs of each e-commerce, and future features needed for expansion. GrandNode is one platform that suits various business models. It can successfully empower traditional B2C & B2B stores, helping local brands expand globally. Our solution supports booking processes, facilitates the management of the availability calendar and even enables the sale of online courses.</p>",
                                             Tags = "e-commerce, grandnode, sample tag, money",
                                             CreatedOnUtc = DateTime.UtcNow.AddSeconds(1),
                                        },
                                };
            await _blogPostRepository.InsertAsync(blogPosts);

            //search engine names
            foreach (var blogPost in blogPosts)
            {
                var seName = SeoExtensions.GenerateSlug(blogPost.Title, false, false, false);
                await _entityUrlRepository.InsertAsync(new EntityUrl
                {
                    EntityId = blogPost.Id,
                    EntityName = "BlogPost",
                    LanguageId = "",
                    IsActive = true,
                    Slug = seName
                });
                blogPost.SeName = seName;
                await _blogPostRepository.UpdateAsync(blogPost);

            }
        }
    }
}
