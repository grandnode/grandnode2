using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Blogs;
using Grand.Web.Admin.Models.Blogs;

namespace Grand.Web.Admin.Extensions
{
    public static class BlogPostMappingExtensions
    {
        public static BlogPostModel ToModel(this BlogPost entity, IDateTimeService dateTimeService)
        {
            var blogpost = entity.MapTo<BlogPost, BlogPostModel>();
            blogpost.CreateDate = entity.CreatedOnUtc.ConvertToUserTime(dateTimeService);
            blogpost.StartDate = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
            blogpost.EndDate = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
            return blogpost;
        }

        public static BlogPost ToEntity(this BlogPostModel model, IDateTimeService dateTimeService)
        {
            var blogpost = model.MapTo<BlogPostModel, BlogPost>();
            blogpost.CreatedOnUtc = model.CreateDate.ConvertToUtcTime(dateTimeService);
            blogpost.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
            blogpost.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
            return blogpost;
        }

        public static BlogPost ToEntity(this BlogPostModel model, BlogPost destination, IDateTimeService dateTimeService)
        {
            var blogpost = model.MapTo(destination);
            blogpost.CreatedOnUtc = model.CreateDate.ConvertToUtcTime(dateTimeService);
            blogpost.StartDateUtc = model.StartDate.ConvertToUtcTime(dateTimeService);
            blogpost.EndDateUtc = model.EndDate.ConvertToUtcTime(dateTimeService);
            return blogpost;
        }
    }
}