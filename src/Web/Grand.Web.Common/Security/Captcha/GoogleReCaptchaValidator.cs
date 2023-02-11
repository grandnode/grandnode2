﻿using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Grand.Web.Common.Security.Captcha
{
    public class GoogleReCaptchaValidator
    {
        private const string RecaptchaVerifyUrl = "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}";

        private readonly CaptchaSettings _captchaSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public GoogleReCaptchaValidator(CaptchaSettings captchaSettings,
            IHttpContextAccessor httpContextAccessor,
            HttpClient httpClient)
        {
            _captchaSettings = captchaSettings;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }

        public async Task<GoogleReCaptchaResponse> Validate(string response)
        {
            GoogleReCaptchaResponse result;
            if (_httpContextAccessor.HttpContext == null) return null;
            var requestUri = string.Format(RecaptchaVerifyUrl, _captchaSettings.ReCaptchaPrivateKey, response,
                _httpContextAccessor.HttpContext.Connection.RemoteIpAddress);

            try
            {
                var responseMessage = await _httpClient.GetAsync(requestUri);
                responseMessage.EnsureSuccessStatusCode();
                var contentResult = await responseMessage.Content.ReadAsStringAsync();
                result = ParseResponseResult(contentResult);

            }
            catch (Exception exc)
            {
                result = new GoogleReCaptchaResponse { IsValid = false };
                result.ErrorCodes.Add("Unknown error" + exc.Message);
            }

            return result;
        }

        private GoogleReCaptchaResponse ParseResponseResult(string responseString)
        {
            var result = new GoogleReCaptchaResponse();

            var resultObject = JObject.Parse(responseString);
            var success = resultObject.Value<bool>("success");
            if(_captchaSettings.ReCaptchaVersion == GoogleReCaptchaVersion.V3)
            {
                var score = resultObject.Value<decimal>("score");
                if (_captchaSettings.ReCaptchaScore > 0)
                    success = success && score >= _captchaSettings.ReCaptchaScore;
            }
            result.IsValid = success;

            if (resultObject.Value<JToken>("error-codes") != null &&
                    resultObject.Value<JToken>("error-codes")!.Values<string>().Any())
                result.ErrorCodes = resultObject.Value<JToken>("error-codes")?.Values<string>().ToList();

            return result;
        }
    }
}