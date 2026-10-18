using ExchangeRate.McExchange;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;

namespace Grand.Modules.Tests.Plugins.McExchange;

[TestClass]
public class McExchangeRateProviderTests
{
    private const string EcbXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01" xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
          <gesmes:subject>Reference rates</gesmes:subject>
          <Cube>
            <Cube time="2026-10-16">
              <Cube currency="USD" rate="1.1650"/>
              <Cube currency="PLN" rate="4.2500"/>
            </Cube>
          </Cube>
        </gesmes:Envelope>
        """;

    private const string NbpXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <ArrayOfExchangeRatesTable xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
          <ExchangeRatesTable>
            <Table>A</Table>
            <EffectiveDate>2026-10-15</EffectiveDate>
            <Rates><Rate><Currency>dolar</Currency><Code>USD</Code><Mid>3.5000</Mid></Rate></Rates>
          </ExchangeRatesTable>
          <ExchangeRatesTable>
            <Table>A</Table>
            <EffectiveDate>2026-10-16</EffectiveDate>
            <Rates><Rate><Currency>dolar</Currency><Code>USD</Code><Mid>4.0000</Mid></Rate></Rates>
          </ExchangeRatesTable>
        </ArrayOfExchangeRatesTable>
        """;

    private sealed class StubHandler(Func<CancellationToken, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => answer(cancellationToken);
    }

    private static McExchangeRateProvider Provider(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(Constant.DefaultHttpClientName)).Returns(() => new HttpClient(handler));
        return new McExchangeRateProvider(factory.Object);
    }

    private static StubHandler Answer(string xml) =>
        new(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(xml) }));

    [TestMethod]
    public async Task GetCurrencyLiveRates_Eur_ReadsEcbRates()
    {
        var rates = await Provider(Answer(EcbXml)).GetCurrencyLiveRates("EUR");

        Assert.HasCount(2, rates);
        Assert.AreEqual("USD", rates[0].CurrencyCode);
        Assert.AreEqual(1.165, rates[0].Rate);
        Assert.AreEqual(new DateTime(2026, 10, 16), rates[0].UpdatedOn);
    }

    [TestMethod]
    public async Task GetCurrencyLiveRates_Pln_ReadsLatestNbpTable()
    {
        var rates = await Provider(Answer(NbpXml)).GetCurrencyLiveRates("PLN");

        Assert.HasCount(1, rates);
        Assert.AreEqual("USD", rates[0].CurrencyCode);
        Assert.AreEqual(0.25, rates[0].Rate);
        Assert.AreEqual(new DateTime(2026, 10, 16), rates[0].UpdatedOn);
    }

    [TestMethod]
    [DataRow("EUR")]
    [DataRow("PLN")]
    public async Task GetCurrencyLiveRates_Cancelled_StopsWaitingForTheBank(string currencyCode)
    {
        var provider = Provider(new StubHandler(async ct => {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.GetCurrencyLiveRates(currencyCode, cts.Token).WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [TestMethod]
    public void HttpClient_IsRegisteredWithShortTimeout()
    {
        var services = new ServiceCollection();
        new StartupApplication().ConfigureServices(services, new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Constant.DefaultHttpClientName);

        Assert.AreEqual(TimeSpan.FromSeconds(30), client.Timeout);
    }
}
