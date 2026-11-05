using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Products;

public class ProductCourseService : IProductCourseService
{
    private readonly IRepository<Course> _courseRepository;
    private readonly IMediator _mediator;
    private readonly IRepository<Product> _productRepository;

    public ProductCourseService(IRepository<Product> productRepository, IRepository<Course> courseRepository,
        IMediator mediator)
    {
        _productRepository = productRepository;
        _courseRepository = courseRepository;
        _mediator = mediator;
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
        ArgumentNullException.ThrowIfNullOrEmpty(productId);

        await _productRepository.UpdateField(productId, x => x.CourseId, courseId);
        await _productRepository.UpdateField(productId, x => x.UpdatedOnUtc, DateTime.UtcNow);

        await _mediator.Publish(new ProductChangedEvent(productId));
    }
}