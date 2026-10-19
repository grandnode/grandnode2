using System.Diagnostics;
using System.Globalization;
using System.Net.Mime;
using System.Xml;

namespace ExchangeRate.McExchange;

internal class NbpExchange : IRateProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NbpExchange(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IList<Grand.Domain.Directory.ExchangeRate>> GetCurrencyLiveRates(
        CancellationToken cancellationToken)
    {
        var currentDate = DateTime.Today.AddDays(-1);
        var httpClient = _httpClientFactory.CreateClient(Constant.DefaultHttpClientName);
        httpClient.DefaultRequestHeaders.Add("Accept", MediaTypeNames.Application.Xml);
        //read the whole body here: the client timeout does not cover a stream read after the headers
        var response = await httpClient.GetStringAsync(
            $"{Constant.NbpUrl}{currentDate.AddDays(-7):yyyy-MM-dd}/{currentDate:yyyy-MM-dd}", cancellationToken);
        var document = new XmlDocument();
        document.LoadXml(response);

        var node = document.SelectNodes("//EffectiveDate")
            .Cast<XmlElement>()
            .OrderByDescending(x => x.InnerText)
            .First();

        var updateDate = DateTime.ParseExact(node.InnerText, "yyyy-MM-dd", null);
        Debug.Assert(node.ParentNode != null, "node.ParentNode != null");
        var ratesNode = node.ParentNode.SelectSingleNode("Rates");

        var provider = new NumberFormatInfo {
            CurrencyDecimalSeparator = ".",
            NumberGroupSeparator = ""
        };

        var exchangeRates = new List<Grand.Domain.Directory.ExchangeRate>();
        foreach (XmlNode node2 in ratesNode.ChildNodes)
        {
            var rate = double.Parse(node2.SelectSingleNode("Mid").InnerText, provider);
            exchangeRates.Add(new Grand.Domain.Directory.ExchangeRate {
                CurrencyCode = node2.SelectSingleNode("Code").InnerText,
                Rate = Math.Round(1 / rate, 4, MidpointRounding.AwayFromZero),
                UpdatedOn = updateDate
            });
        }

        return exchangeRates;
    }
}