﻿using Grand.Business.Catalog.Services.Directory;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Directory
{
    [TestClass()]
    public class GeoLookupServiceTests
    {
        private Mock<ILogger<GeoLookupService>> _loggerMock;
        private GeoLookupService _service;
        private string IPAddressUS = "142.250.203.206";
        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "../../../../../Web/Grand.Web/";
            _loggerMock = new Mock<ILogger<GeoLookupService>>();
            _service = new GeoLookupService(_loggerMock.Object);
        }


        [TestMethod()]
        public void CountryIsoCodeTest()
        {
            var countryiso = _service.CountryIsoCode(IPAddressUS);
            Assert.AreEqual("US", countryiso);
        }

        [TestMethod()]
        public void CountryNameTest()
        {
            var countryname = _service.CountryName(IPAddressUS);
            Assert.AreEqual("United States", countryname);
        }
    }
}