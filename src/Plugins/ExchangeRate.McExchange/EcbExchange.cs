using System.Globalization;
using System.Xml;

namespace ExchangeRate.McExchange;

internal class EcbExchange : IRateProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EcbExchange(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IList<Grand.Domain.Directory.ExchangeRate>> GetCurrencyLiveRates(
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(Constant.DefaultHttpClientName);
        //read the whole body here: the client timeout does not cover a stream read after the headers
        var response = await httpClient.GetStringAsync(Constant.EcbUrl, cancellationToken);
        var document = new XmlDocument();
        document.LoadXml(response);
        var nameTable = new XmlNamespaceManager(document.NameTable);
        nameTable.AddNamespace("ns", "http://www.ecb.int/vocabulary/2002-08-01/eurofxref");
        nameTable.AddNamespace("gesmes", "http://www.gesmes.org/xml/2002-08-01");

        var node = document.SelectSingleNode("gesmes:Envelope/ns:Cube/ns:Cube", nameTable);
        var updateDate = DateTime.ParseExact(node.Attributes["time"].Value, "yyyy-MM-dd", null);

        var provider = new NumberFormatInfo {
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = ""
        };

        var exchangeRates = new List<Grand.Domain.Directory.ExchangeRate>();
        foreach (XmlNode node2 in node.ChildNodes)
            exchangeRates.Add(new Grand.Domain.Directory.ExchangeRate {
                    CurrencyCode = node2.Attributes["currency"].Value,
                    Rate = double.Parse(node2.Attributes["rate"].Value, provider),
                    UpdatedOn = updateDate
                }
            );
        return exchangeRates;
    }
}