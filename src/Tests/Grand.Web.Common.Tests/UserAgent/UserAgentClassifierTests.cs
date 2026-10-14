using Grand.Web.Common.UserAgent;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.UserAgent;

[TestClass]
public class UserAgentClassifierTests
{
    private static UserAgentClassifier ClassifierFor(string userAgent)
    {
        var context = new DefaultHttpContext();
        if (userAgent != null) context.Request.Headers.UserAgent = userAgent;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        return new UserAgentClassifier(accessor.Object);
    }

    [TestMethod]
    [DataRow("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [DataRow("Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)")]
    [DataRow("Mozilla/5.0 (compatible; AhrefsBot/7.0; +http://ahrefs.com/robot/)")]
    [DataRow("Mozilla/5.0 (compatible; SemrushBot/7~bl; +http://www.semrush.com/bot.html)")]
    [DataRow("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; GPTBot/1.2; +https://openai.com/gptbot)")]
    [DataRow("Mozilla/5.0 AppleWebKit/537.36 (KHTML, like Gecko; compatible; ClaudeBot/1.0; +claudebot@anthropic.com)")]
    [DataRow("Mozilla/5.0 (compatible; YandexBot/3.0; +http://yandex.com/bots)")]
    [DataRow("Mozilla/5.0 (Linux; Android 5.0) AppleWebKit/537.36 (KHTML, like Gecko) Mobile Safari/537.36 (compatible; Bytespider; spider-feedback@bytedance.com)")]
    [DataRow("Mozilla/5.0 (compatible; Baiduspider/2.0; +http://www.baidu.com/search/spider.html)")]
    [DataRow("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [DataRow("meta-externalagent/1.1 (+https://developers.facebook.com/docs/sharing/webmasters/crawler)")]
    [DataRow("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/120.0.0.0 Safari/537.36")]
    [DataRow("Mozilla/5.0 (compatible; crawler)")]
    [DataRow("Mozilla/5.0 (compatible; SomeSpider/1.0)")]
    [DataRow("Scrapy/2.11.0 (+https://scrapy.org)")]
    [DataRow("curl/8.4.0")]
    [DataRow("Wget/1.21")]
    [DataRow("python-requests/2.31.0")]
    [DataRow("Go-http-client/1.1")]
    [DataRow("okhttp/4.12.0")]
    [DataRow("Java/17.0.2")]
    [DataRow("Apache-HttpClient/4.5.14 (Java/17.0.2)")]
    [DataRow("axios/1.6.0")]
    [DataRow("node-fetch/1.0 (+https://github.com/bitinn/node-fetch)")]
    [DataRow("")]
    [DataRow(null)]
    public void IsCrawler_BotsAndHttpClients_True(string userAgent)
    {
        Assert.IsTrue(ClassifierFor(userAgent).IsCrawler);
    }

    [TestMethod]
    [DataRow("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")]
    [DataRow("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15")]
    [DataRow("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1")]
    [DataRow("Mozilla/5.0 (Linux; Android 10; CUBOT X30) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36")]
    [DataRow("Mozilla/5.0 (X11; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0")]
    public void IsCrawler_Browsers_False(string userAgent)
    {
        Assert.IsFalse(ClassifierFor(userAgent).IsCrawler);
    }

    [TestMethod]
    [DataRow("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148", true)]
    [DataRow("Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/604.1", true)]
    [DataRow("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", true)]
    [DataRow("Mozilla/5.0 (Linux; Android 13; SM-X710) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", true)]
    [DataRow("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", false)]
    [DataRow("", false)]
    public void IsMobileOrTablet(string userAgent, bool expected)
    {
        Assert.AreEqual(expected, ClassifierFor(userAgent).IsMobileOrTablet);
    }

    [TestMethod]
    public void NoHttpContext_NotACrawlerNotMobile()
    {
        //a scope without a request (scheduled task) is never a crawler
        var classifier = new UserAgentClassifier(Mock.Of<IHttpContextAccessor>());

        Assert.IsFalse(classifier.IsCrawler);
        Assert.IsFalse(classifier.IsMobileOrTablet);
    }
}
