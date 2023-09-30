using AutoMapper;
using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Domain.Admin;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Mapper;
using Grand.Web.Admin.Models.Menu;
using Grand.Web.Admin.Services;
using Moq;
using NUnit.Framework;

namespace Grand.Web.Admin.Tests;

    [TestFixture]
    public class MenuViewModelServiceTests
    {
        private MenuViewModelService _menuViewModelService;
        private Mock<IAdminSiteMapService> _adminSiteMapServiceMock;
        private IMapper _mapper;
        
        [SetUp]
        public void SetUp()
        {
            _adminSiteMapServiceMock = new Mock<IAdminSiteMapService>();
            _menuViewModelService = new MenuViewModelService(_adminSiteMapServiceMock.Object);
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MenuProfile());
            });
            _mapper = mapperConfig.CreateMapper();
            AutoMapperConfig.Init(mapperConfig);
        }

        [Test]
        public async Task MenuItems_ReturnsMenuItems()
        {
            // Arrange
            var siteMap = new List<AdminSiteMap>
            {
                new AdminSiteMap { Id = "1", SystemName = "Item 1", DisplayOrder = 1 },
                new AdminSiteMap { Id = "2", SystemName = "Item 2", DisplayOrder = 2 }
            };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(siteMap);

            // Act
            var result = await _menuViewModelService.MenuItems();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1", result[0].Id);
            Assert.AreEqual("Item 1", result[0].SystemName);
            Assert.AreEqual(1, result[0].DisplayOrder);
            Assert.AreEqual("2", result[1].Id);
            Assert.AreEqual("Item 2", result[1].SystemName);
            Assert.AreEqual(2, result[1].DisplayOrder);
        }

        [Test]
        public async Task GetMenuById_ReturnsMenuById()
        {
            // Arrange
            var siteMap = new List<AdminSiteMap>
            {
                new AdminSiteMap { Id = "1", SystemName = "Item 1" },
                new AdminSiteMap { Id = "2", SystemName = "Item 2" }
            };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(siteMap);

            // Act
            var result = await _menuViewModelService.GetMenuById("2");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("2", result.Id);
            Assert.AreEqual("Item 2", result.SystemName);
        }
        [Test]
        public async Task InsertMenuModel_AddsNewMenuModel()
        {
            // Arrange
            var model = new MenuModel { Id = "3", SystemName = "Item 3" };
            var parentEntity = new AdminSiteMap { Id = "1", SystemName = "Item 1", ChildNodes = new List<AdminSiteMap>() };
            var sitemap = new List<AdminSiteMap> { parentEntity };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(sitemap);
            _adminSiteMapServiceMock.Setup(x => x.InsertSiteMap(It.IsAny<AdminSiteMap>())).Callback<AdminSiteMap>(node => parentEntity.ChildNodes.Add(node));

            // Act
            var result = await _menuViewModelService.InsertMenuModel(model, "1");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("3", result.Id);
            Assert.AreEqual("Item 3", result.SystemName);
            Assert.AreEqual(1, parentEntity.ChildNodes.Count);
        }
        
        [Test]
        public async Task UpdateMenuModel_UpdatesMenuModel()
        {
            // Arrange
            var model = new MenuModel { Id = "2", SystemName = "Updated Item 2" };
            var siteMap = new List<AdminSiteMap>
            {
                new AdminSiteMap { Id = "1", SystemName = "Item 1" },
                new AdminSiteMap { Id = "2", SystemName = "Item 2" }
            };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(siteMap);
            _adminSiteMapServiceMock.Setup(x => x.UpdateSiteMap(It.IsAny<AdminSiteMap>()));
            
            // Act
            var result = await _menuViewModelService.UpdateMenuModel(model);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("2", result.Id);
            Assert.AreEqual("Updated Item 2", result.SystemName);
        }
        [Test]
        public async Task UpdateMenuModel_UpdatesSubMenuModel()
        {
            // Arrange
            var model = new MenuModel { Id = "3", SystemName = "Updated Item 3" };
            var siteMap = new List<AdminSiteMap>
            {
                new AdminSiteMap { Id = "1", SystemName = "Item 1" },
                new AdminSiteMap { Id = "2", SystemName = "Item 2", 
                    ChildNodes = new List<AdminSiteMap>(){new (){Id = "3", SystemName = "Item 3"} }}
            };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(siteMap);
            _adminSiteMapServiceMock.Setup(x => x.UpdateSiteMap(It.IsAny<AdminSiteMap>()));
            
            // Act
            var result = await _menuViewModelService.UpdateMenuModel(model);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("3", result.Id);
            Assert.AreEqual("Updated Item 3", result.SystemName);
        }
        [Test]
        public async Task DeleteMenu_CallsDeleteSiteMap()
        {
            // Arrange
            var idToDelete = "2";
            var siteMap = new List<AdminSiteMap>
            {
                new AdminSiteMap { Id = "1", SystemName = "Item 1", ChildNodes = new List<AdminSiteMap>() },
                new AdminSiteMap { Id = "2", SystemName = "Item 2" }
            };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(siteMap);
    
            // Act
            await _menuViewModelService.DeleteMenu(idToDelete);

            // Assert
            _adminSiteMapServiceMock.Verify(x => x.DeleteSiteMap(It.IsAny<AdminSiteMap>()), Times.Once);
        }
        [Test]
        public async Task DeleteMenu_CallsUpdate()
        {
            // Arrange
            var idToDelete = "3";
            var siteMap = new List<AdminSiteMap>
            {
                new AdminSiteMap { Id = "1", SystemName = "Item 1", ChildNodes = new List<AdminSiteMap>() { new AdminSiteMap(){ Id = idToDelete } } },
                new AdminSiteMap { Id = "2", SystemName = "Item 2" }
            };

            _adminSiteMapServiceMock.Setup(x => x.GetSiteMap()).ReturnsAsync(siteMap);

            // Act
            await _menuViewModelService.DeleteMenu(idToDelete);

            // Assert
            _adminSiteMapServiceMock.Verify(x => x.UpdateSiteMap(It.IsAny<AdminSiteMap>()), Times.Once);
        }
    }