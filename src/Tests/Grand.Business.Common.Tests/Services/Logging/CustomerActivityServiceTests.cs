using Grand.Business.Common.Services.Logging;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Data;
using Grand.Domain.Logging;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Logging
{
    [TestClass()]
    public class CustomerActivityServiceTests
    {
        private IRepository<ActivityLog> _repository;
        private IRepository<ActivityLogType> _repositoryActivityLogType;
        private Mock<IMediator> _mediatorMock;
        private MemoryCacheBase _cacheBase;
        
        private CustomerActivityService _customerActivityService;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<ActivityLog>();
            _repositoryActivityLogType = new MongoDBRepositoryTest<ActivityLogType>();

            _mediatorMock = new Mock<IMediator>();
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object, new CacheConfig(){ DefaultCacheTimeMinutes = 1});
            _customerActivityService = new CustomerActivityService(_cacheBase, _repository, _repositoryActivityLogType);
        }

        private async Task InsertActivityType()
        {
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Id = "1", Enabled = true, Name = "test", SystemKeyword = "test" });
        }

        [TestMethod()]
        public async Task InsertActivityTypeTest()
        {
            //Act
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Enabled = true, Name = "test", SystemKeyword = "test" });
            //Assert
            Assert.IsTrue(_repositoryActivityLogType.Table.Any());
        }

        [TestMethod()]
        public async Task UpdateActivityTypeTest()
        {
            //Arrange
            var activityLogType = new ActivityLogType();
            await _customerActivityService.InsertActivityType(activityLogType);

            //Act
            activityLogType.Enabled = true;
            await _customerActivityService.UpdateActivityType(activityLogType);
            //Assert
            Assert.IsTrue(_repositoryActivityLogType.Table.FirstOrDefault(x=>x.Id == activityLogType.Id).Enabled);
        }

        [TestMethod()]
        public async Task DeleteActivityTypeTest()
        {
            //Arrange
            var activityLogType = new ActivityLogType();
            await _customerActivityService.InsertActivityType(activityLogType);

            //Act
            await _customerActivityService.DeleteActivityType(activityLogType);
            //Assert
            Assert.IsNull(_repositoryActivityLogType.Table.FirstOrDefault(x => x.Id == activityLogType.Id));
        }

        [TestMethod()]
        public async Task GetAllActivityTypesTest()
        {
            //Arrange
            var activityLogType = new ActivityLogType();
            await _customerActivityService.InsertActivityType(activityLogType);

            //Act
            var result = await _customerActivityService.GetAllActivityTypes();
            //Assert
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod()]
        public async Task GetActivityTypeByIdTest()
        {
            //Arrange
            var activityLogType = new ActivityLogType();
            await _customerActivityService.InsertActivityType(activityLogType);

            //Act
            var result = await _customerActivityService.GetActivityTypeById(activityLogType.Id);
            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task InsertActivityTest()
        {
            await InsertActivityType();
            //Act
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1"});
            //Assert
            //TODO
            Assert.IsTrue(true);
        }

        [TestMethod()]
        public async Task DeleteActivityTest()
        {
            await InsertActivityType();
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });
            var activityLogs = await _customerActivityService.GetAllActivities();
            //Act
            await _customerActivityService.DeleteActivity(activityLogs.FirstOrDefault());
            //Assert
            Assert.IsFalse(_repository.Table.Any());
        }

        [TestMethod()]
        public async Task GetAllActivitiesTest()
        {
            //Arrange
            await InsertActivityType();
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });
            //Act
            var activityLogs = await _customerActivityService.GetAllActivities();
            //Assert
            //TODO
            Assert.IsTrue(activityLogs.Count > 0);
        }

        [TestMethod()]
        public async Task GetStatsActivitiesTest()
        {
            //Arrange
            await InsertActivityType();
            await _repository.InsertAsync(new ActivityLog() {  ActivityLogTypeId = "1", EntityKeyId = "1" });
            //Act
            var activityLogs = await _customerActivityService.GetStatsActivities();
            //Assert
            Assert.AreEqual(1, activityLogs.Count);
        }

        [TestMethod()]
        public async Task GetCategoryActivitiesTest()
        {
            //Arrange
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Id = "1", Enabled = true, Name = "AddNewCategory", SystemKeyword = "AddNewCategory" });
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });

            //Act
            var activities = await _customerActivityService.GetCategoryActivities(categoryId:"1");
            //Assert
            Assert.IsTrue(activities.Count > 0);
        }

        [TestMethod()]
        public async Task GetKnowledgebaseCategoryActivitiesTest()
        {
            //Arrange
            await _customerActivityService.InsertActivityType(new ActivityLogType() {Id = "1", Enabled = true, Name = "CreateKnowledgebaseCategory", SystemKeyword = "CreateKnowledgebaseCategory" });
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });
            //Act
            var activities = await _customerActivityService.GetKnowledgebaseCategoryActivities(categoryId:"1");
            //Assert
            Assert.IsTrue(activities.Count > 0);

        }

        [TestMethod()]
        public async Task GetKnowledgebaseArticleActivitiesTest()
        {
            //Arrange
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Id = "1", Enabled = true, Name = "CreateKnowledgebaseArticle", SystemKeyword = "CreateKnowledgebaseArticle" });
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });

            //Act
            var activities = await _customerActivityService.GetKnowledgebaseArticleActivities(categoryId:"1");
            //Assert
            Assert.IsTrue(activities.Count > 0);
        }

        [TestMethod()]
        public async Task GetBrandActivitiesTest()
        {
            //Arrange
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Id = "1", Enabled = true, Name = "AddNewBrand", SystemKeyword = "AddNewBrand" });
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });

            //Act
            var activities = await _customerActivityService.GetBrandActivities(brandId:"1");
            //Assert
            Assert.IsTrue(activities.Count > 0);
        }

        [TestMethod()]
        public async Task GetCollectionActivitiesTest()
        {
            //Arrange
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Id = "1", Enabled = true, Name = "AddNewCollection", SystemKeyword = "AddNewCollection" });
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });
            //Act
            var activities = await _customerActivityService.GetCollectionActivities(collectionId:"1");
            //Assert
            Assert.IsTrue(activities.Count > 0);
        }

        [TestMethod()]
        public async Task GetProductActivitiesTest()
        {
            //Arrange
            await _customerActivityService.InsertActivityType(new ActivityLogType() { Id = "1", Enabled = true, Name = "AddNewProduct", SystemKeyword = "AddNewProduct" });
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });

            //Act
            var activities = await _customerActivityService.GetProductActivities(productId:"1");
            //Assert
            Assert.IsTrue(activities.Count > 0);
        }

        [TestMethod()]
        public async Task GetActivityByIdTest()
        {
            //Arrange
            await InsertActivityType();
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });
            //Act
            var activityLogs = await _customerActivityService.GetAllActivities();
            var result = await _customerActivityService.GetActivityById(activityLogs[0].Id);
            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task ClearAllActivitiesTest()
        {
            //Arrange
            await _repository.InsertAsync(new ActivityLog() { ActivityLogTypeId = "1", EntityKeyId = "1" });
            //Act
            await _customerActivityService.ClearAllActivities();
            //Assert
            Assert.IsFalse(_repository.Table.Any());
        }
    }
}