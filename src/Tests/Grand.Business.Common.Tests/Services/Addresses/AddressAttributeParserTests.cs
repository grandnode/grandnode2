using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Services.Addresses;
using Grand.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Addresses
{
    [TestClass()]
    public class AddressAttributeParserTests
    {
        private Mock<IAddressAttributeService> _atrService;
        private Mock<ITranslationService> _translationService;
        private AddressAttributeParser _parser;
        private List<CustomAttribute> customAtr;

        [TestInitialize()]
        public void Init()
        {
            _atrService = new Mock<IAddressAttributeService>();
            _translationService = new Mock<ITranslationService>();
            _parser = new AddressAttributeParser(_atrService.Object,_translationService.Object);
            customAtr = new List<CustomAttribute>()
            {
                new CustomAttribute(){Key="key1",Value="value1" },
                new CustomAttribute(){Key="key2",Value="value2" },
                new CustomAttribute(){Key="key3",Value="value3" },
                new CustomAttribute(){Key="key4",Value="value4" },
            };
        }

        [TestMethod()]
        public async Task ParseAddressAttributes_ReturnExpectedValues()
        {
            _atrService.Setup(c => c.GetAddressAttributeById("key1")).Returns(Task.FromResult(new AddressAttribute() { Id = "key1" }));
            _atrService.Setup(c => c.GetAddressAttributeById("key2")).Returns(Task.FromResult(new AddressAttribute() { Id = "key2" }));
            var result = await _parser.ParseAddressAttributes(customAtr);
            Assert.IsTrue(result.Count == 2);
            Assert.IsTrue(result.First().Id.Equals("key1"));
        }

        [TestMethod()]
        public void AddAddressAttribute_ReturnExpectedValues()
        {
            var atr = new AddressAttribute() { Id = "added" };
            var result = _parser.AddAddressAttribute(customAtr, atr, "new-added-value");
            Assert.IsTrue(result.Count == 5);
            Assert.IsTrue(result.Any(c => c.Key.Equals("added")));
        }
    }
}
