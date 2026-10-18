using Grand.Domain.Common;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Captcha;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Grand.Web.Common.Security.Captcha;

public class GoogleReCaptchaValidator : IGoogleReCaptchaValidator
{
    private const string RecaptchaVerifyUrl =
        "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}";

    private readonly CaptchaSettings _captchaSettings;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GoogleReCaptchaValidator> _logger;

    public GoogleReCaptchaValidator(CaptchaSettings captchaSettings,
        IHttpContextAccessor httpContextAccessor,
        HttpClient httpClient,
        ILogger<GoogleReCaptchaValidator> logger)
    {
        _captchaSettings = captchaSettings;
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GoogleReCaptchaResponse> Validate(string response,
        CancellationToken cancellationToken = default)
    {
        GoogleReCaptchaResponse result;
        if (_httpContextAccessor.HttpContext == null) return null;
        var requestUri = string.Format(RecaptchaVerifyUrl, _captchaSettings.ReCaptchaPrivateKey, response,
            _httpContextAccessor.HttpContext.Connection.RemoteIpAddress);

        try
        {
            using var responseMessage = await _httpClient.GetAsync(requestUri, cancellationToken);
            responseMessage.EnsureSuccessStatusCode();
            var contentResult = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            result = ParseResponseResult(contentResult);
        }
        catch (Exception exc) when (exc is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            //Google failed or timed out: keep the details in the log, not in the visitor's validation message
            _logger.LogWarning(exc, "reCAPTCHA verification failed");
            result = new GoogleReCaptchaResponse { Success = false };
            result.ErrorCodes.Add("Unknown error");
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