using Grand.Domain.Catalog;
using Grand.Domain.Courses;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    public interface IProductCourseService
    {
        Task<Product> GetProductByCourseId(string courseId);
        Task<Course> GetCourseByProductId(string productId);
        Task UpdateCourseOnProduct(string productId, string courseId);
    }
}
