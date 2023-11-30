using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Payments.PayPalStandard.Services
{
    public class PaypalHttpClient : IPaypalHttpClient
    {
        private readonly HttpClient _client;
        private readonly PayPalStandardPaymentSettings _paypalStandardPaymentSettings;
        private readonly ILogger<PaypalHttpClient> _logger;
        public PaypalHttpClient(HttpClient client, PayPalStandardPaymentSettings paypalStandardPaymentSettings, ILogger<PaypalHttpClient> logger)
        {
            _client = client;
            _paypalStandardPaymentSettings = paypalStandardPaymentSettings;
            _logger = logger;
        }

        /// <summary>
        /// Gets IPN PayPal URL
        /// </summary>
        /// <returns></returns>
        private string GetIpnPaypalUrl()
        {
            return _paypalStandardPaymentSettings.UseSandbox ?
                "https://ipnpb.sandbox.paypal.com/cgi-bin/webscr" :
                "https://ipnpb.paypal.com/cgi-bin/webscr";
        }

        public async Task<(bool success, Dictionary<string, string> values)> VerifyIpn(string formString)
        {
            var formContent = new StringContent($"cmd=_notify-validate&{formString}",
                Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _client.PostAsync(GetIpnPaypalUrl(), formContent);
            var content = string.Empty;
            try
            {
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyIpn");
            }


            var success = content.Trim().Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in formString.Split('&'))
            {
                var line = l.Trim();
                var equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line[..equalPox], line[(equalPox + 1)..]);
            }
            return (success, values);
        }

        /// <summary>
        /// Gets PayPal PDT URL
        /// </summary>
        /// <returns></returns>
        private string GetPaypalPdtUrl()
        {
            return _paypalStandardPaymentSettings.UseSandbox ?
                PaypalHelper.PayPalUrlSandbox :
                PaypalHelper.PayPalUrl;
        }

        public async Task<(bool status, Dictionary<string, string> values, string response)> GetPdtDetails(string tx)
        {
            var formContent = new StringContent($"cmd=_notify-synch&at={_paypalStandardPaymentSettings.PdtToken}&tx={tx}",
               Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _client.PostAsync(GetPaypalPdtUrl(), formContent);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "GetPdtDetails");
            }
            var content = await response.Content.ReadAsStringAsync();

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool firstLine = true, success = false;
            foreach (var l in content.Split('\n'))
            {
                var line = l.Trim();
                if (firstLine)
                {
                    success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    firstLine = false;
                }
                else
                {
                    var equalPox = line.IndexOf('=');
                    if (equalPox >= 0)
                        values.Add(line[..equalPox], line[(equalPox + 1)..]);
                }
            }

            return (success, values, content);
        }
    }
}
