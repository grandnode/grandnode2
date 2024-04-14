using Grand.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text.Json;

namespace Grand.Web.Common.Security.Captcha;

public class GoogleReCaptchaValidator
{
    private const string RecaptchaVerifyUrl =
        "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}";

    private readonly CaptchaSettings _captchaSettings;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

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
            result = new GoogleReCaptchaResponse { Success = false };
            result.ErrorCodes.Add("Unknown error" + exc.Message);
        }

        return result;
    }

    private GoogleReCaptchaResponse ParseResponseResult(string responseString)
    {
        var result = JsonSerializer.Deserialize<GoogleReCaptchaResponse>(responseString,
            ProviderExtensions.JsonSerializerOptionsProvider.Options);
        if (_captchaSettings.ReCaptchaVersion != GoogleReCaptchaVersion.V3) return result;

        if (_captchaSettings.ReCaptchaScore > 0)
            result.Success = result.Success && result.Score >= _captchaSettings.ReCaptchaScore;

        return result;
    }
}