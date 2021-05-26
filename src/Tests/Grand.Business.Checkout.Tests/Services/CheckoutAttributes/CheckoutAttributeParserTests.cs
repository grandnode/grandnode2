using Grand.Business.Checkout.Interfaces.CheckoutAttributes;
using Grand.Business.Checkout.Services.CheckoutAttributes;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Domain.Data.Mongo;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Business.Checkout.Tests.Services.CheckoutAttributes
{
    [TestClass()]
    public class CheckoutAttributeParserTests
    {
        private IRepository<CheckoutAttribute> _checkoutAttributeRepo;
        private IMediator _eventPublisher;
        private Mock<ICacheBase> cacheManager;
        private Mock<IWorkContext> _workContextMock;
        private ICheckoutAttributeService _checkoutAttributeService;
        private ICheckoutAttributeParser _checkoutAttributeParser;

        private CheckoutAttribute ca1, ca2, ca3;
        private CheckoutAttributeValue cav1_1, cav1_2, cav2_1, cav2_2;

        [TestInitialize()]
        public void TestInitialize()
        {
            CommonPath.BaseDirectory = "";

            //color choosing via DropDownList
            ca1 = new CheckoutAttribute {
                Id = "1",
                Name = "Color",
                TextPrompt = "Select color:",
                IsRequired = true,
                AttributeControlTypeId = AttributeControlType.DropdownList,
                DisplayOrder = 1,
            };
            cav1_1 = new CheckoutAttributeValue {
                Id = "11",
                Name = "Green",
                DisplayOrder = 1,
                //CheckoutAttribute = ca1,
                CheckoutAttributeId = ca1.Id,
            };
            cav1_2 = new CheckoutAttributeValue {
                Id = "12",
                Name = "Red",
                DisplayOrder = 2,
                //CheckoutAttribute = ca1,
                CheckoutAttributeId = ca1.Id,
            };
            ca1.CheckoutAttributeValues.Add(cav1_1);
            ca1.CheckoutAttributeValues.Add(cav1_2);

            //choosing via CheckBox'es
            ca2 = new CheckoutAttribute {
                Id = "2",
                Name = "Custom option",
                TextPrompt = "Select custom option:",
                IsRequired = true,
                AttributeControlTypeId = AttributeControlType.Checkboxes,
                DisplayOrder = 2,
                //CheckoutAttributeValues
            };

            cav2_1 = new CheckoutAttributeValue {
                Id = "21",
                Name = "Option 1",
                DisplayOrder = 1,
                //CheckoutAttribute = ca2,
                CheckoutAttributeId = ca2.Id,
            };
            cav2_2 = new CheckoutAttributeValue {
                Id = "22",
                Name = "Option 2",
                DisplayOrder = 2,
                //CheckoutAttribute = ca2,
                CheckoutAttributeId = ca2.Id,
            };
            ca2.CheckoutAttributeValues.Add(cav2_1);
            ca2.CheckoutAttributeValues.Add(cav2_2);

            //via MultiTextBoxes
            ca3 = new CheckoutAttribute {
                Id = "3",
                Name = "Custom text",
                TextPrompt = "Enter custom text:",
                IsRequired = true,
                AttributeControlTypeId = AttributeControlType.MultilineTextbox,
                DisplayOrder = 3,
            };

            var tempEventPublisher = new Mock<IMediator>();
            {
                //tempEventPublisher.Setup(x => x.PublishAsync(It.IsAny<object>()));
                _eventPublisher = tempEventPublisher.Object;
            }

            var tempCheckoutAttributeRepo = new Mock<IRepository<CheckoutAttribute>>();
            {
                var IMongoCollection = new Mock<MongoRepository<CheckoutAttribute>>().Object;
                IMongoCollection.Insert(ca1);
                IMongoCollection.Insert(ca2);
                IMongoCollection.Insert(ca3);
                tempCheckoutAttributeRepo.Setup(x => x.Table).Returns(IMongoCollection.Table);
                tempCheckoutAttributeRepo.Setup(x => x.GetByIdAsync(ca1.Id)).ReturnsAsync(ca1);
                tempCheckoutAttributeRepo.Setup(x => x.GetByIdAsync(ca2.Id)).ReturnsAsync(ca2);
                tempCheckoutAttributeRepo.Setup(x => x.GetByIdAsync(ca3.Id)).ReturnsAsync(ca3);
                _checkoutAttributeRepo = tempCheckoutAttributeRepo.Object;
            }

            cacheManager = new Mock<ICacheBase>();
            _workContextMock = new Mock<IWorkContext>();

            _checkoutAttributeService = new CheckoutAttributeService(cacheManager.Object, _checkoutAttributeRepo,
               _eventPublisher, _workContextMock.Object);

            _checkoutAttributeParser = new CheckoutAttributeParser(_checkoutAttributeService);



            var workingLanguage = new Language();
            var tempWorkContext = new Mock<IWorkContext>();
            {
                tempWorkContext.Setup(x => x.WorkingLanguage).Returns(workingLanguage);
            }
        }

        [TestMethod()]
        public void Can_add_checkoutAttributes()
        {
            IList<CustomAttribute> attributes = new List<CustomAttribute>();
            //color: green
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca1, cav1_1.Id.ToString()).ToList();
            //custom option: option 1, option 2
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_1.Id.ToString()).ToList();
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_2.Id.ToString()).ToList();
            //custom text
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca3, "absolutely any value").ToList();

            Assert.IsTrue(attributes.Count == 4);
            Assert.IsTrue(attributes.Any(c => c.Key.Equals(ca1.Id)));
            Assert.IsTrue(attributes.Any(c => c.Key.Equals(ca2.Id)));
            Assert.IsTrue(attributes.Any(c => c.Key.Equals(ca3.Id)));
            attributes = _checkoutAttributeParser.RemoveCheckoutAttribute(attributes, ca1);
            Assert.IsTrue(attributes.Count == 3);
            Assert.IsFalse(attributes.Any(c => c.Key.Equals(ca1.Id)));

        }
    }
}
