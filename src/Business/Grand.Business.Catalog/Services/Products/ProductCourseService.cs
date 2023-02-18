﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Domain.Data;

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
            return await Task.FromResult(_courseRepository.Table.FirstOrDefault(p => p.ProductId == productId));
        }

        public virtual async Task<Product> GetProductByCourseId(string courseId)
        {
            return await Task.FromResult(_productRepository.Table.FirstOrDefault(p => p.CourseId == courseId));
        }

        public virtual async Task UpdateCourseOnProduct(string productId, string courseId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentNullException(nameof(productId));

            await _productRepository.UpdateField(productId, x => x.CourseId, courseId);
            await _productRepository.UpdateField(productId, x => x.UpdatedOnUtc, DateTime.UtcNow);
        }
    }
}
