﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Catalog.Services.Directory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Catalog.Services.Directory.Tests
{
    [TestClass()]
    public class GeoLookupServiceTests
    {
        private Mock<ILogger> _loggerMock;
        private GeoLookupService _service;
        private string IPAddressUS = "142.250.203.206";
        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "../../../../../Web/Grand.Web/";
            _loggerMock = new Mock<ILogger>();
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