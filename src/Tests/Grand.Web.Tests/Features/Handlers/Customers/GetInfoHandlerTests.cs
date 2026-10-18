using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Customers;

[TestClass]
public class GetInfoHandlerTests
{
    private const string Oidc = "ExternalAuth.OpenIdConnect";
    private const string Google = "ExternalAuth.Google";

    private static async Task<CustomerInfoModel> Handle(OidcLoginConfig config)
    {
        var external = new Mock<IExternalAuthenticationService>();
        external.Setup(x => x.LoadActiveAuthenticationProviders(It.IsAny<Customer>(), It.IsAny<Domain.Stores.Store>()))
            .Returns(new List<IExternalAuthenticationProvider>());
        external.Setup(x => x.GetExternalIdentifiers(It.IsAny<Customer>())).ReturnsAsync(
            (IList<ExternalAuthentication>)new List<ExternalAuthentication> {
                new() { Id = "1", ProviderSystemName = Oidc },
                new() { Id = "2", ProviderSystemName = Google }
            });
        external.Setup(x => x.AuthenticationProviderIsAvailable(It.IsAny<string>(), It.IsAny<Grand.Domain.Customers.Customer>(), It.IsAny<Grand.Domain.Stores.Store>())).Returns(true);
        var providerMock = new Mock<IExternalAuthenticationProvider>();
        providerMock.Setup(p => p.FriendlyName).Returns("Provider");
        external.Setup(x => x.LoadAuthenticationProviderBySystemName(It.IsAny<string>())).Returns(providerMock.Object);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetCustomAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerAttributeModel>());
        var newsletterCategories = new Mock<INewsletterCategoryService>();
        newsletterCategories.Setup(s => s.GetAllNewsletterCategory()).ReturnsAsync(new List<NewsletterCategory>());

        var handler = new GetInfoHandler(new Mock<INewsLetterSubscriptionService>().Object,
            newsletterCategories.Object, new Mock<IRequestTranslationService>().Object, new Mock<ICountryService>().Object,
            external.Object, mediator.Object,
            new CustomerSettings { AllowUsersToChangeEmail = true, TwoFactorAuthenticationEnabled = true },
            new TaxSettings(), new Mock<IEnumTranslationService>().Object, config);

        return await handler.Handle(new GetInfo {
            Customer = new Customer { Id = "c1", Email = "a@b.c" },
            Store = new Domain.Stores.Store { Id = "s1" },
            Language = new Language { Id = "l1" }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Info_ProviderOnly_EmailReadOnlyTwoFactorHidden()
    {
        var model = await Handle(new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly });
        Assert.IsFalse(model.AllowUsersToChangeEmail);
        Assert.IsFalse(model.TwoFactorAvailable);
    }

    [TestMethod]
    public async Task Info_Mixed_EmailEditableTwoFactorShown()
    {
        var model = await Handle(new OidcLoginConfig());
        Assert.IsTrue(model.AllowUsersToChangeEmail);
        Assert.IsTrue(model.TwoFactorAvailable);
    }

    [TestMethod]
    public async Task Info_ProviderOnly_OidcRecordNotRemovable_GoogleRemovable()
    {
        var model = await Handle(new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly });
        Assert.IsFalse(model.AssociatedExternalAuthRecords.Single(x => x.Id == "1").CanRemove);
        Assert.IsTrue(model.AssociatedExternalAuthRecords.Single(x => x.Id == "2").CanRemove);
    }

    [TestMethod]
    public async Task Info_Mixed_AllRecordsRemovable()
    {
        var model = await Handle(new OidcLoginConfig());
        Assert.IsTrue(model.AssociatedExternalAuthRecords.All(x => x.CanRemove));
    }
}
