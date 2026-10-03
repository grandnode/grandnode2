using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.Web.Features.Handlers.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Customers;

[TestClass]
public class GetRegisterHandlerTests
{
    private GetRegisterHandler _handler;

    [TestInitialize]
    public void Init()
    {
        var countryServiceMock = new Mock<ICountryService>();
        countryServiceMock.Setup(s => s.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Country> { new() { Id = "pl", Name = "Poland" }, new() { Id = "de", Name = "Germany" } });
        countryServiceMock.Setup(s => s.GetStateProvincesByCountryId(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<StateProvince>());
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerAttributeModel>());
        var newsletterCategoryServiceMock = new Mock<INewsletterCategoryService>();
        newsletterCategoryServiceMock.Setup(s => s.GetNewsletterCategoriesByStore(It.IsAny<string>()))
            .ReturnsAsync(new List<NewsletterCategory>());

        _handler = new GetRegisterHandler(newsletterCategoryServiceMock.Object, new Mock<ITranslationService>().Object,
            countryServiceMock.Object, mediatorMock.Object,
            new CustomerSettings { CountryEnabled = true, StateProvinceEnabled = true },
            new TaxSettings(), new CaptchaSettings());
    }

    private Task<RegisterModel> Handle(string defaultCountryId, RegisterModel model = null)
    {
        return _handler.Handle(new GetRegister {
            Model = model,
            Customer = new Customer(),
            Language = new Language { Id = "lang" },
            Store = new Domain.Stores.Store { Id = "store", DefaultCountryId = defaultCountryId }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task NewRegistration_StartsInTheStoreDefaultCountry()
    {
        var model = await Handle("de");

        Assert.AreEqual("de", model.CountryId);
        Assert.AreEqual("de", model.AvailableCountries.Single(x => x.Selected).Value);
    }

    [TestMethod]
    public async Task PostedCountry_IsKept()
    {
        var model = await Handle("de", new RegisterModel { CountryId = "pl" });

        Assert.AreEqual("pl", model.CountryId);
    }

    [TestMethod]
    public async Task DefaultCountryNotOffered_LeavesTheCountryEmpty()
    {
        var model = await Handle("us");

        Assert.IsNull(model.CountryId);
    }
}
