namespace ExchangeRate.McExchange
{
    public static class Constant
    {
        public static string DefaultHttpClientName => "ExchangeRateHttpClient";
        public static string EcbUrl => "http://www.ecb.int/stats/eurofxref/eurofxref-daily.xml";
        public static string NbpUrl => "http://api.nbp.pl/api/exchangerates/tables/A/";
    }
}
