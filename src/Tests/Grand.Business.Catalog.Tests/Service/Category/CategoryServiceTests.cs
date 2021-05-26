using Grand.Business.Catalog.Services.Categories;
using Grand.Business.Common.Interfaces.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Grand.Business.Catalog.Tests.Service.Category
{
    [TestClass()]
    public class CategoryServiceTests
    {
        private Mock<ICacheBase> _casheManagerMock;
        private Mock<IRepository<Grand.Domain.Catalog.Category>> _categoryRepositoryMock;
        private Mock<MongoRepository<Product>> _productRepositoryMock;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IAclService> _aclServiceMock;
        private ProductCategoryService _productCategoryService;
        private CatalogSettings _settings;
        private CategoryService _categoryService;

        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "";

            _casheManagerMock = new Mock<ICacheBase>();
            _categoryRepositoryMock = new Mock<IRepository<Grand.Domain.Catalog.Category>>();
            _productRepositoryMock = new Mock<MongoRepository<Product>>();
            _workContextMock = new Mock<IWorkContext>();
            _mediatorMock = new Mock<IMediator>();
            _aclServiceMock = new Mock<IAclService>();
            _settings = new CatalogSettings();
            _categoryService = new CategoryService(_casheManagerMock.Object, _categoryRepositoryMock.Object, _workContextMock.Object,
                 _mediatorMock.Object, _aclServiceMock.Object);
            _productCategoryService = new ProductCategoryService(_productRepositoryMock.Object, _casheManagerMock.Object, _workContextMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public void InsertCategory_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _categoryService.InsertCategory(null), "category");
        }

        [TestMethod()]
        public async Task InsertCategory_ValidArgument_InvokeRepositoryAndCache()
        {
            await _categoryService.InsertCategory(new Grand.Domain.Catalog.Category());
            _categoryRepositoryMock.Verify(c => c.InsertAsync(It.IsAny<Domain.Catalog.Category>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Grand.Domain.Catalog.Category>>(), default(CancellationToken)), Times.Once);
            _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(2));
        }


        [TestMethod()]
        public void UpdateCategory_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _categoryService.UpdateCategory(null), "category");
        }

        [TestMethod()]
        public async Task UpdateCategory_ValidArgument_InvokeRepositoryAndCache()
        {
            await _categoryService.UpdateCategory(new Grand.Domain.Catalog.Category());
            _categoryRepositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Grand.Domain.Catalog.Category>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Grand.Domain.Catalog.Category>>(), default(CancellationToken)), Times.Once);
            _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(2));
        }

        [TestMethod()]
        public void GetCategoryBreadCrumb_ShouldReturnEmptyList()
        {
            var allCategory = GetMockCategoryList();
            var category = new Grand.Domain.Catalog.Category() { ParentCategoryId = "3" };
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<Customer>())).Returns(() => true);
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<string>())).Returns(() => true);
            var result = _categoryService.GetCategoryBreadCrumb(category, allCategory);
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod()]
        public void GetCategoryBreadCrumb_ShouldReturnTwoElement()
        {
            var allCategory = GetMockCategoryList();
            var category = new Grand.Domain.Catalog.Category() { Id = "6", ParentCategoryId = "3", Published = true };
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "" });
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<Customer>())).Returns(() => true);
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<string>())).Returns(() => true);
            var result = _categoryService.GetCategoryBreadCrumb(category, allCategory);
            Assert.IsTrue(result.Count == 2);
            Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("3")));
        }

        [TestMethod()]
        public void GetCategoryBreadCrumb_ShouldReturnThreeElement()
        {
            var allCategory = GetMockCategoryList();
            var category = new Grand.Domain.Catalog.Category() { Id = "6", ParentCategoryId = "1", Published = true };
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "" });
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<Customer>())).Returns(() => true);
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<string>())).Returns(() => true);
            var result = _categoryService.GetCategoryBreadCrumb(category, allCategory);
            Assert.IsTrue(result.Count == 3);
            Assert.IsTrue(result.Any(c => c.Id.Equals("6")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("1")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("5")));
        }

        [TestMethod()]
        public void GetFormattedBreadCrumb_ReturnExprectedString()
        {
            var exprectedString = "cat5 >> cat1 >> cat6";
            var allCategory = GetMockCategoryList();
            var category = new Grand.Domain.Catalog.Category() { Id = "6", Name = "cat6", ParentCategoryId = "1", Published = true };
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<Customer>())).Returns(() => true);
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<string>())).Returns(() => true);
            var result = _categoryService.GetFormattedBreadCrumb(category, allCategory);
            Assert.IsTrue(exprectedString.Equals(result));
        }

        [TestMethod()]
        public void GetFormattedBreadCrumb_ReturnEmptyString()
        {
            var allCategory = GetMockCategoryList();
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<Customer>())).Returns(() => true);
            _aclServiceMock.Setup(a => a.Authorize<Grand.Domain.Catalog.Category>(It.IsAny<Grand.Domain.Catalog.Category>(), It.IsAny<string>())).Returns(() => true);
            var result = _categoryService.GetFormattedBreadCrumb(null, allCategory);
            Assert.IsTrue(string.IsNullOrEmpty(result));
        }

        [TestMethod()]
        public async Task DeleteProductCategory_InvokreRepositoryAndClearCache()
        {
            //var collectonMock = new Mock<IMongoCollection<Product>>();
            //_productRepositoryMock.Setup(p => p.Collection).Returns(collectonMock.Object);
            await _productCategoryService.DeleteProductCategory(new ProductCategory(), "1");
            //TODO
            //collectonMock.Verify(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<Product>>(), It.IsAny<UpdateDefinition<Product>>(), null, default(CancellationToken)), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<ProductCategory>>(), default(CancellationToken)), Times.Once);
            //clear product cache and ProductCategory
            _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(2));
        }

        [TestMethod()]
        public void DeleteProductCategory_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _productCategoryService.DeleteProductCategory(null, "id"), "productCategory");
        }

        [TestMethod()]
        public async Task InsertProductCategory_InvokreRepositoryAndClearCache()
        {
            //var collectonMock = new Mock<IMongoCollection<Product>>();
            //_productRepositoryMock.Setup(p => p.Collection).Returns(collectonMock.Object);
            await _productCategoryService.InsertProductCategory(new ProductCategory(), "id");
            //TODO
            //collectonMock.Verify(c => c.UpdateOneAsync(It.IsAny<FilterDefinition<Product>>(), It.IsAny<UpdateDefinition<Product>>(), null, default(CancellationToken)), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<ProductCategory>>(), default(CancellationToken)), Times.Once);
            _casheManagerMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(2));
        }

        [TestMethod()]
        public void InsertProductCategory_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _productCategoryService.InsertProductCategory(null, "id"), "productCategory");
        }

        private IList<Grand.Domain.Catalog.Category> GetMockCategoryList()
        {
            return new List<Grand.Domain.Catalog.Category>()
            {
                new Grand.Domain.Catalog.Category(){ Id="1" ,Name="cat1",Published=true,ParentCategoryId="5"},
                new Grand.Domain.Catalog.Category(){ Id="2" ,Name="cat2",Published=true},
                new Grand.Domain.Catalog.Category(){ Id="3" ,Name="cat3",Published=true},
                new Grand.Domain.Catalog.Category(){ Id="4" ,Name="cat4",Published=true},
                new Grand.Domain.Catalog.Category(){ Id="5" ,Name="cat5",Published=true},
            };
        }

    }
}
