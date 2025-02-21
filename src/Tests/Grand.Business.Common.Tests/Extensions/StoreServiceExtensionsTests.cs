using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Extensions
{
    [TestClass]
    public class StoreServiceExtensionsTests
    {
        [TestMethod]
        public async Task GetStoreByHostOrStoreId_NullStoreId_CallsGetStoreByHost()
        {
            // Arrange
            var expectedStore = new Store { Url = "http://storehost.com" };
            var storeServiceMock = new Mock<IStoreService>();
            storeServiceMock.Setup(x => x.GetStoreByHost("storehost.com"))
                .ReturnsAsync(expectedStore);

            // Act
            var result = await storeServiceMock.Object.GetStoreByHostOrStoreId("storehost.com", null);

            // Assert
            Assert.AreEqual(expectedStore, result);
            storeServiceMock.Verify(x => x.GetStoreByHost("storehost.com"), Times.Once);
        }

        [TestMethod]
        public async Task GetStoreByHostOrStoreId_WithValidStoreId_ReturnsStoreById()
        {
            // Arrange
            var expectedStore = new Store { Url = "http://storeid.com" };
            var storeServiceMock = new Mock<IStoreService>();
            storeServiceMock.Setup(x => x.GetStoreById("123"))
                .ReturnsAsync(expectedStore);

            // Act
            var result = await storeServiceMock.Object.GetStoreByHostOrStoreId("anyhost.com", "123");

            // Assert
            Assert.AreEqual(expectedStore, result);
            storeServiceMock.Verify(x => x.GetStoreById("123"), Times.Once);
        }

        [TestMethod]
        public async Task GetStoreByHostOrStoreId_WithInvalidStoreId_ReturnsFirstStoreFromAllStores()
        {
            // Arrange
            var storeFromAll = new Store { Url = "http://firststore.com" };
            var stores = new List<Store>
            {
                storeFromAll,
                new Store { Url = "http://secondstore.com" }
            };

            var storeServiceMock = new Mock<IStoreService>();
            // GetStoreById returns null when storeId is not found
            storeServiceMock.Setup(x => x.GetStoreById("notfound"))
                .ReturnsAsync((Store)null);
            storeServiceMock.Setup(x => x.GetAllStores())
                .ReturnsAsync(stores);

            // Act
            var result = await storeServiceMock.Object.GetStoreByHostOrStoreId("anyhost.com", "notfound");

            // Assert
            Assert.AreEqual(storeFromAll, result);
            storeServiceMock.Verify(x => x.GetStoreById("notfound"), Times.Once);
            storeServiceMock.Verify(x => x.GetAllStores(), Times.Once);
        }
    }
}
