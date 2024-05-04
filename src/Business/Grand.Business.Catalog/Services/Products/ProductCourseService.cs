using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;

namespace Grand.Business.Catalog.Services.Products;

public class ProductCourseService : IProductCourseService
{
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<Product> _productRepository;

    public ProductCourseService(IRepository<Product> productRepository, IRepository<Course> courseRepository)
    {
        _productRepository = productRepository;
        _courseRepository = courseRepository;
    }

    public virtual async Task<Course> GetCourseByProductId(string productId)
    {
        return await _courseRepository.GetOneAsync(p => p.ProductId == productId);
    }

    public virtual async Task<Product> GetProductByCourseId(string courseId)
    {
        return await _productRepository.GetOneAsync(p => p.CourseId == courseId);
    }

    public virtual async Task UpdateCourseOnProduct(string productId, string courseId)
    {
        if (string.IsNullOrEmpty(productId))
            throw new ArgumentNullException(nameof(productId));

        await _productRepository.UpdateField(productId, x => x.CourseId, courseId);
        await _productRepository.UpdateField(productId, x => x.UpdatedOnUtc, DateTime.UtcNow);
    }
}