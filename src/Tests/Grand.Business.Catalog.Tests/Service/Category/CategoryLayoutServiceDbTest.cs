using Grand.Business.Catalog.Services.Categories;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Service.Category
{
    [TestClass()]
    public class CategoryLayoutServiceDbTest
    {
        private IRepository<CategoryLayout> _repostiory;
        private MemoryCacheBase _cacheBase;
        private Mock<IMediator> _mediatorMock;
        private CategoryLayoutService _categoryLayoutService;

        [TestInitialize()]
        public void Init()
        {
            _repostiory = new MongoDBRepositoryTest<CategoryLayout>();
            _mediatorMock = new Mock<IMediator>();

            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            _categoryLayoutService = new CategoryLayoutService(_repostiory, _cacheBase, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task GetCategoryLayoutById()
        {
            //Arrange
            var categoryLayout1 = new CategoryLayout() {
                Name = "test1"
            };
            await _categoryLayoutService.InsertCategoryLayout(categoryLayout1);

            //Assert
            var layout = await _categoryLayoutService.GetCategoryLayoutById(categoryLayout1.Id);

            //Act
            Assert.IsNotNull(layout);
            Assert.AreEqual("test1", layout.Name);
        }
        [TestMethod()]
        public async Task GetAllCategoryLayouts()
        {
            //Arrange
            await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
            await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
            await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());

            //Assert
            var layouts = await _categoryLayoutService.GetAllCategoryLayouts();

            //Act
            Assert.AreEqual(3, layouts.Count);
        }

        [TestMethod()]
        public async Task DeleteCategoryLayout()
        {
            //Arrange
            var categoryLayout1 = new CategoryLayout() {
                Name = "test1"
            };
            await _categoryLayoutService.InsertCategoryLayout(categoryLayout1);
            var categoryLayout2 = new CategoryLayout() {
                Name = "test2"
            };
            await _categoryLayoutService.InsertCategoryLayout(categoryLayout2);

            //Assert
            await _categoryLayoutService.DeleteCategoryLayout(categoryLayout1);

            //Act
            Assert.IsNull(_repostiory.Table.FirstOrDefault(x => x.Name == "test1"));
            Assert.AreEqual(1, _repostiory.Table.Count());

        }


        [TestMethod()]
        public async Task InsertCategoryLayout_True()
        {
            //Assert
            await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
            //Act
            Assert.IsTrue(_repostiory.Table.Any());
        }

        [TestMethod()]
        public async Task UpdateCategoryLayout_IsNotNull()
        {
            //Arrange
            var categoryLayout = new CategoryLayout() {
                Name = "test"
            };
            await _categoryLayoutService.InsertCategoryLayout(categoryLayout);
            categoryLayout.Name = "test2";

            //Assert
            await _categoryLayoutService.UpdateCategoryLayout(categoryLayout);

            //Act
            Assert.IsNotNull(_repostiory.Table.FirstOrDefault(x => x.Name == "test2"));
        }

        [TestMethod()]
        public async Task UpdateCategoryLayout_IsNull()
        {
            //Arrange
            var categoryLayout = new CategoryLayout() {
                Name = "test"
            };
            await _categoryLayoutService.InsertCategoryLayout(categoryLayout);
            categoryLayout.Name = "test2";

            //Assert
            await _categoryLayoutService.UpdateCategoryLayout(categoryLayout);

            //Act
            Assert.IsNull(_repostiory.Table.FirstOrDefault(x => x.Name == "test3"));
        }
    }
}
