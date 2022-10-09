﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Catalog.Events.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Domain.Data;
using Grand.Domain.Catalog;
using Grand.Data.Tests.MongoDb;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Moq;
using Grand.Domain.Customers;
using Grand.Domain.Seo;

namespace Grand.Business.Catalog.Events.Handlers.Tests
{
    [TestClass()]
    public class ProductDeletedEventHandlerTests
    {
        private IRepository<Product> _repository;
        private IRepository<CustomerGroupProduct> _customerGroupProductRepository;
        private IRepository<Customer> _customerRepository;
        private IRepository<EntityUrl> _entityUrlRepository;
        private IRepository<ProductTag> _productTagRepository;
        private IRepository<ProductReview> _productReviewRepository;
        private IRepository<ProductDeleted> _productDeletedRepository;
        private Mock<IProductTagService> _productTagService;
        private ProductDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Product>();
            _customerGroupProductRepository = new MongoDBRepositoryTest<CustomerGroupProduct>();
            _customerRepository = new MongoDBRepositoryTest<Customer>();
            _entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
            _productTagRepository = new MongoDBRepositoryTest<ProductTag>();
            _productReviewRepository = new MongoDBRepositoryTest<ProductReview>();
            _productDeletedRepository = new MongoDBRepositoryTest<ProductDeleted>();
            _productTagService = new Mock<IProductTagService>();
            _handler = new ProductDeletedEventHandler(_repository, _customerGroupProductRepository,
                _customerRepository, _entityUrlRepository, _productTagRepository, _productReviewRepository,
                _productDeletedRepository, _productTagService.Object);
        }


        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var product = new Product();
            product.RecommendedProduct.Add("1");
            product.RelatedProducts.Add(new RelatedProduct() { ProductId2 = "1" });
            product.CrossSellProduct.Add("1");
            product.SimilarProducts.Add(new SimilarProduct() { ProductId2 = "1" });
            await _repository.InsertAsync(product);
            //Act
            await _handler.Handle(new Infrastructure.Events.EntityDeleted<Product>(new Product() { Id = "1" }), CancellationToken.None);

            //Assert
            var result = _repository.Table.FirstOrDefault(x => x.Id == product.Id);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.RelatedProducts.Count == 0);
            Assert.IsTrue(result.RecommendedProduct.Count == 0);
            Assert.IsTrue(result.CrossSellProduct.Count == 0);
            Assert.IsTrue(result.SimilarProducts.Count == 0);
        }
    }
}