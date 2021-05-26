using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Domain.Data;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Grand.Business.Catalog.Services.Products
{
    public class ProductCourseService : IProductCourseService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Course> _courseRepository;

        public ProductCourseService(IRepository<Product> productRepository, IRepository<Course> courseRepository)
        {
            _productRepository = productRepository;
            _courseRepository = courseRepository;
        }

        public virtual async Task<Course> GetCourseByProductId(string productId)
        {
            return await Task.FromResult(_courseRepository.Table.Where(p => p.ProductId == productId).FirstOrDefault());
        }

        public virtual async Task<Product> GetProductByCourseId(string courseId)
        {
            return await Task.FromResult(_productRepository.Table.Where(p => p.CourseId == courseId).FirstOrDefault());
        }

        public virtual async Task UpdateCourseOnProduct(string productId, string courseId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentNullException("productId");

            await _productRepository.UpdateField(productId, x => x.CourseId, courseId);
            await _productRepository.UpdateField(productId, x => x.UpdatedOnUtc, DateTime.UtcNow);
        }
    }
}
