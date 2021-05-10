using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Grand.Web.Common.Security.Captcha
{
    public class GoogleReCaptchaValidator
    {
        private const string RECAPTCHA_VERIFY_URL = "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}";

        public string SecretKey { get; set; }
        public string RemoteIp { get; set; }
        public string Response { get; set; }
        public string Challenge { get; set; }

        private readonly GoogleReCaptchaVersion _version;

        public GoogleReCaptchaValidator(GoogleReCaptchaVersion version = GoogleReCaptchaVersion.V2)
        {
            _version = version;
        }

        public async Task<GoogleReCaptchaResponse> Validate()
        {
            GoogleReCaptchaResponse result = null;
            var httpClient = new HttpClient();
            var requestUri = string.Format(RECAPTCHA_VERIFY_URL, SecretKey, Response, RemoteIp);

            try
            {
                var response = await httpClient.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();
                var contentresult = await response.Content.ReadAsStringAsync();
                result = ParseResponseResult(contentresult);

            }
            catch (Exception exc)
            {
                result = new GoogleReCaptchaResponse { IsValid = false };
                result.ErrorCodes.Add("Unknown error" + exc.Message);
            }
            finally
            {
                httpClient.Dispose();
            }

            return result;
        }

        private GoogleReCaptchaResponse ParseResponseResult(string responseString)
        {
            var result = new GoogleReCaptchaResponse();

            var resultObject = JObject.Parse(responseString);
            result.IsValid = resultObject.Value<bool>("success");

            if (resultObject.Value<JToken>("error-codes") != null &&
                    resultObject.Value<JToken>("error-codes").Values<string>().Any())
                result.ErrorCodes = resultObject.Value<JToken>("error-codes").Values<string>().ToList();

            return result;
        }
    }
}