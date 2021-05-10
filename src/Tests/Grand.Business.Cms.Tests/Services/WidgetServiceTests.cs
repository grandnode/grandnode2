using Grand.Business.Cms.Interfaces;
using Grand.Business.Cms.Services;
using Grand.Domain.Cms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Tests.Services
{
    [TestClass()]
    public class WidgetServiceTests
    {
        private WidgetSettings _settings;
        private IWidgetService _widgedService;
        private Mock<IWidgetProvider> _widgetProvider1;
        private Mock<IWidgetProvider> _widgetProvider2;

        [TestInitialize()]
        public void Init()
        {
            _widgetProvider1 = new Mock<IWidgetProvider>();
            _widgetProvider1.Setup(c => c.SystemName).Returns("name1");
            _widgetProvider1.Setup(c => c.GetWidgetZones().Result).Returns(new List<string>() { "widgetzone1" });
            _widgetProvider2 = new Mock<IWidgetProvider>();
            _widgetProvider2.Setup(c => c.SystemName).Returns("name2");
            _widgetProvider2.Setup(c => c.GetWidgetZones().Result).Returns(new List<string>() { "widgetzone2" });
            var providers = new List<IWidgetProvider>() { _widgetProvider1.Object, _widgetProvider2.Object };
            _settings = new WidgetSettings();
            _widgedService = new WidgetService(providers, _settings);
        }

        [TestMethod()]
        public void LoadActiveWidgets_SettingsNotContainsSystemName_ReturnEmptyList()
        {
            _settings.ActiveWidgetSystemNames = new List<string>();
            var result = _widgedService.LoadActiveWidgets();
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod()]
        public void LoadActiveWidgets_SettingsContainsSystemName_ReturnList()
        {
            _settings.ActiveWidgetSystemNames = new List<string>() { "name1", "name2" };
            var result = _widgedService.LoadActiveWidgets();
            Assert.IsTrue(result.Count == _settings.ActiveWidgetSystemNames.Count);
        }

        [TestMethod()]
        public async Task LoadActiveWidgetsByWidgetZone_EmptyWidgetZone_ReturnEmptyList()
        {
            var result = await _widgedService.LoadActiveWidgetsByWidgetZone("");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod()]
        public async Task LoadActiveWidgetsByWidgetZone()
        {
            _settings.ActiveWidgetSystemNames = new List<string>() { "name1", "name2" };
            var result = await _widgedService.LoadActiveWidgetsByWidgetZone("widgetZone1");
            Assert.IsTrue(result.Count ==1);
        }

        [TestMethod()]
        public void LoadWidgetBySystemName_ReturnExpectedValue()
        {
            var result = _widgedService.LoadWidgetBySystemName("name1");
            Assert.IsNotNull(result);
        }
    }
}
