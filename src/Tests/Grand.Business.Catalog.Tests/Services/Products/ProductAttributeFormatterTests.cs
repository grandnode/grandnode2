﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Catalog.Services.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Infrastructure;
using Moq;
using MediatR;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.SharedKernel.Extensions;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Customers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Catalog.Services.Products.Tests
{
    [TestClass()]
    public class ProductAttributeFormatterTests
    {
        private ProductAttributeFormatter _productAttributeFormatter;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IMediator> _mediatorMock;
        private IAclService _aclServiceMock;
        private Mock<IProductAttributeService> _productAttributeServiceMock;
        private Mock<ITranslationService> _translationServiceMock;
        private Mock<ITaxService> _taxServiceMock;
        private Mock<IPriceFormatter> _priceFormatterMock;
        private Mock<IPricingService> _pricingServiceMock;
        private Mock<IProductService> _productServiceMock;
        [TestInitialize()]
        public void InitializeTests()
        {
            CommonPath.BaseDirectory = "";

            _workContextMock = new Mock<IWorkContext>();
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
            _productAttributeServiceMock = new Mock<IProductAttributeService>();
            _productAttributeServiceMock.Setup(x => x.GetProductAttributeById(It.IsAny<string>())).Returns(Task.FromResult(new ProductAttribute() { Name = "test" }));
            _translationServiceMock = new Mock<ITranslationService>();
            _translationServiceMock.Setup(x => x.GetResource("GiftVoucherAttribute.From.Virtual")).Returns("Name {0}, Email {1}");
            _translationServiceMock.Setup(x => x.GetResource("GiftVoucherAttribute.For.Virtual")).Returns("RecName {0}, RecEmail {1}");
            _taxServiceMock = new Mock<ITaxService>();
            _priceFormatterMock = new Mock<IPriceFormatter>();
            _pricingServiceMock = new Mock<IPricingService>();
            _productServiceMock = new Mock<IProductService>();
            _mediatorMock = new Mock<IMediator>();
            _aclServiceMock = new AclService();
            _productAttributeFormatter = new ProductAttributeFormatter(_workContextMock.Object, _productAttributeServiceMock.Object,
                _translationServiceMock.Object, _taxServiceMock.Object, _priceFormatterMock.Object, _pricingServiceMock.Object,
                _productServiceMock.Object);
        }

        [TestMethod()]
        public async Task FormatAttributesTest_EmptyCustomAttribute()
        {
            //Arrange
            var product = new Product();
            //Act
            var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute>());
            //Assert
            Assert.AreEqual("", result);
        }
        [TestMethod()]
        public async Task FormatAttributesTest_GiftVoucher()
        {
            //Arrange
            var product = new Product() { GiftVoucherTypeId = GiftVoucherType.Virtual, IsGiftVoucher = true };
            //Act
            var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute>()
            {
                    new CustomAttribute() { Key = "RecipientName", Value =  "John" },
                    new CustomAttribute() { Key = "RecipientEmail", Value =  "John@john.com" },
                    new CustomAttribute() { Key = "SenderName", Value =  "Will" },
                    new CustomAttribute() { Key = "SenderEmail", Value =  "Will@will.com" },
                    new CustomAttribute() { Key = "Message", Value =  "" },
            });
            //Assert
            Assert.AreEqual("Name Will, Email Will@will.com<br />RecName John, RecEmail John@john.com", result);
        }
        [TestMethod()]
        public async Task FormatAttributesTest_CustomAttribute()
        {
            //Arrange
            var product = new Product();
            var productAttributeMapping = new ProductAttributeMapping() { Id = "1", AttributeControlTypeId = AttributeControlType.Checkboxes };
            productAttributeMapping.ProductAttributeValues.Add(new ProductAttributeValue() { Id = "1", Name = "aa" });
            product.ProductAttributeMappings.Add(productAttributeMapping);
            //Act
            var result = await _productAttributeFormatter.FormatAttributes(product, new List<CustomAttribute>()
            {
                    new CustomAttribute() { Key = "1", Value =  "1" },
            });
            //Assert
            Assert.AreEqual("test: aa", result);
        }
    }
}