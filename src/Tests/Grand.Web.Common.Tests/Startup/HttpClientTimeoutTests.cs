using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Startup;

[TestClass]
public class HttpClientTimeoutTests
{
    private static IHttpClientFactory Factory(Action<IServiceCollection> registerMore = null)
    {
        var services = new ServiceCollection();
        new StartupApplication().ConfigureServices(services, new ConfigurationBuilder().Build());
        registerMore?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
    }

    //a plugin client registered without a timeout no longer runs on the 100 s framework default
    [TestMethod]
    public void ClientWithoutOwnTimeout_GetsPlatformDefault()
    {
        var factory = Factory(services => services.AddHttpClient("plugin"));

        Assert.AreEqual(TimeSpan.FromSeconds(30), factory.CreateClient("plugin").Timeout);
    }

    [TestMethod]
    public void ReCaptchaClient_KeepsItsShorterTimeout()
    {
        var factory = Factory();

        Assert.AreEqual(TimeSpan.FromSeconds(10), factory.CreateClient(nameof(IGoogleReCaptchaValidator)).Timeout);
    }
}
