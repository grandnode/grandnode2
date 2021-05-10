using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Services;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Tests.Services
{
    [TestClass()]
    public class CustomerAttributeParserTests
    {
        private Mock<ICustomerAttributeService> _customerAtrServiceMock;
        private Mock<ITranslationService> _translationServiceMock;
        private CustomerAttributeParser _parser;
        private List<CustomAttribute> customAtr;
        private List<CustomerAttribute> _cusomerAtr;

        [TestInitialize()]
        public void Init()
        {
            _customerAtrServiceMock = new Mock<ICustomerAttributeService>();
            _translationServiceMock = new Mock<ITranslationService>();
            _parser = new CustomerAttributeParser(_customerAtrServiceMock.Object, _translationServiceMock.Object);
            customAtr = new List<CustomAttribute>()
            {
                new CustomAttribute(){Key="key1",Value="value1" },
                new CustomAttribute(){Key="key2",Value="value2" },
                new CustomAttribute(){Key="key3",Value="value3" },
                new CustomAttribute(){Key="key4",Value="value4" },
            };

            _cusomerAtr = new List<CustomerAttribute>()
            {
                new CustomerAttribute(){Id="key1"},
                new CustomerAttribute(){Id="key2"},
                new CustomerAttribute(){Id="key3"},
                new CustomerAttribute(){Id="key4"},
                new CustomerAttribute(){Id="key5"},
            };
            _cusomerAtr.ForEach(c => c.CustomerAttributeValues.Add(new CustomerAttributeValue() { Id = "value" + c.Id.Last() }));
        }

        [TestMethod()]
        public async Task ParseCustomerAttributes_ReturnExpectedValues()
        {
            _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) => Task.FromResult(_cusomerAtr.FirstOrDefault(a => a.Id.Equals(w))));
            var result = await _parser.ParseCustomerAttributes(customAtr);
            Assert.IsTrue(result.Count == 4);
            Assert.IsTrue(result.Any(c => c.Id.Equals("key1")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("key2")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("key3")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("key4")));
            Assert.IsFalse(result.Any(c => c.Id.Equals("key5")));
        }

        [TestMethod()]
        public async Task ParseCustomerAttributes_ReturnEmptyList()
        {
            //not exist
            _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns(()=>Task.FromResult<CustomerAttribute>(null));
            var result = await _parser.ParseCustomerAttributes(customAtr);
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod()]
        public async Task ParseCustomerAttributeValues_ReturnExpectedValues()
        {
            _customerAtrServiceMock.Setup(c => c.GetCustomerAttributeById(It.IsAny<string>())).Returns((string w) => Task.FromResult(_cusomerAtr.FirstOrDefault(a => a.Id.Equals(w))));
            var result = await _parser.ParseCustomerAttributeValues(customAtr);
            Assert.IsTrue(result.Count == 4);
            Assert.IsTrue(result.Any(c => c.Id.Equals("value1")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("value2")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("value3")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("value4")));
            Assert.IsFalse(result.Any(c => c.Id.Equals("value5")));
        }


        [TestMethod()]
        public void AddCustomerAttribute_ReturnExpectedValues()
        {
            //not exist
            
            var result =  _parser.AddCustomerAttribute(customAtr,new CustomerAttribute() { Id="key7"},"value7");
            Assert.IsTrue(result.Count==5);
            Assert.IsTrue(result.Any(c=>c.Key.Equals("key7")));
        }
    }
}
