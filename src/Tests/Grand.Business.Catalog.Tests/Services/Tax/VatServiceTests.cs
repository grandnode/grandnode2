﻿using Grand.Domain.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Services.Tax.Tests
{
    [TestClass()]
    public class VatServiceTests
    {
        private VatService _vatService;
        private TaxSettings _taxSettings;

        [TestInitialize()]
        public void Init()
        {
            _taxSettings = new TaxSettings() { EuVatAssumeValid = true, EuVatUseWebService = false };
            _vatService = new VatService(_taxSettings);
        }

        [DataRow("PL1111111111")]
        [DataRow("1111111111")]
        [TestMethod()]
        public async Task GetVatNumberStatusTest(string vat)
        {
            //Act
            var result = await _vatService.GetVatNumberStatus(vat);
            //Assert
            Assert.AreEqual(VatNumberStatus.Valid, result.status);
        }
        [TestMethod()]
        public async Task GetVatNumberStatusTest_Valid()
        {
            //Act
            var result = await _vatService.GetVatNumberStatus("PL", "1111111111");
            //Assert
            Assert.AreEqual(VatNumberStatus.Valid, result.status);
        }
        [TestMethod()]
        public async Task GetVatNumberStatusTest_InValid()
        {
            //Act
            var result = await _vatService.GetVatNumberStatus("PL", "");
            //Assert
            Assert.AreEqual(VatNumberStatus.Empty, result.status);
        }
        [TestMethod()]
        public async Task DoVatCheckTest()
        {
            //Act
            var result = await _vatService.DoVatCheck("PL", "1111111111");
            //Assert
            Assert.AreEqual(VatNumberStatus.Invalid, result.status);
        }

        [TestMethod()]
        public async Task CheckVatRequestTest()
        {
            //Act
            var result = await _vatService.CheckVatRequest(new Core.Utilities.Catalog.VatRequest());
            //Assert
            Assert.IsNotNull(result);
        }
    }
}